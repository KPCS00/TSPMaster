using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Funds;
using TSPMaster.API.Models;

namespace TSPMaster.API.Services;

public class TspDataService : ITspDataService
{
    // These column names match exactly what tsp.gov CSV provides
    private static readonly string[] AllFundNames =
    [
        "L Income", "L 2030", "L 2035", "L 2040", "L 2045",
        "L 2050", "L 2055", "L 2060", "L 2065", "L 2070", "L 2075",
        "G Fund", "F Fund", "C Fund", "S Fund", "I Fund"
    ];

    private static readonly Dictionary<string, string> FundDescriptions = new()
    {
        ["G Fund"] = "Government Securities Investment Fund — U.S. Treasury securities, lowest risk",
        ["F Fund"] = "Fixed Income Index Fund — Bloomberg U.S. Aggregate Bond Index",
        ["C Fund"] = "Common Stock Index Fund — S&P 500 large-cap stocks",
        ["S Fund"] = "Small Cap Stock Index Fund — Dow Jones U.S. Completion Total Market",
        ["I Fund"] = "International Stock Index Fund — MSCI EAFE (Europe, Australasia, Far East)",
        ["L Income"] = "Lifecycle Income Fund — Conservative, for those at or near retirement",
        ["L 2030"] = "Lifecycle 2030 Fund — Target date 2028–2032",
        ["L 2035"] = "Lifecycle 2035 Fund — Target date 2033–2037",
        ["L 2040"] = "Lifecycle 2040 Fund — Target date 2038–2042",
        ["L 2045"] = "Lifecycle 2045 Fund — Target date 2043–2047",
        ["L 2050"] = "Lifecycle 2050 Fund — Target date 2048–2052",
        ["L 2055"] = "Lifecycle 2055 Fund — Target date 2053–2057",
        ["L 2060"] = "Lifecycle 2060 Fund — Target date 2058–2062",
        ["L 2065"] = "Lifecycle 2065 Fund — Target date 2063–2067",
        ["L 2070"] = "Lifecycle 2070 Fund — Target date 2068–2072",
        ["L 2075"] = "Lifecycle 2075 Fund — Target date 2073+",
    };

    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TspDataService> _logger;

    public TspDataService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TspDataService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public IReadOnlyList<string> GetAllFundNames() => AllFundNames;

    public async Task EnsurePricesUpToDateAsync(CancellationToken cancellationToken = default)
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var latestExisting = await _db.FundPrices
            .MaxAsync(f => (DateOnly?)f.Date, cancellationToken);

        if (!latestExisting.HasValue || latestExisting.Value < targetDate)
        {
            _logger.LogInformation("Fund prices out of date (latest DB date: {Latest}, target: {Target}). Triggering price sync.",
                latestExisting?.ToString("yyyy-MM-dd") ?? "none", targetDate.ToString("yyyy-MM-dd"));
            await SyncAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Fund prices are up to date through {Latest}.", latestExisting.Value.ToString("yyyy-MM-dd"));
        }
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var csvUrl = _config["TspData:CsvUrl"] ?? "https://www.tsp.gov/data/fund-price-history.csv";
        _logger.LogInformation("Starting TSP price sync from {Url}", csvUrl);

        var client = _httpClientFactory.CreateClient("TspClient");

        string csvContent;
        try
        {
            csvContent = await client.GetStringAsync(csvUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download TSP CSV from {Url}", csvUrl);
            return;
        }

        // Find the latest date we already have to skip re-importing
        var latestExisting = await _db.FundPrices
            .MaxAsync(f => (DateOnly?)f.Date, cancellationToken);

        var newRows = new List<FundPrice>();
        int totalRows = 0, skipped = 0;

        using var reader = new StringReader(csvContent);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var csv = new CsvReader(reader, csvConfig);
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            totalRows++;
            var dateStr = csv.GetField<string>("Date");
            if (!DateOnly.TryParse(dateStr, out var date)) continue;
            if (latestExisting.HasValue && date <= latestExisting.Value)
            {
                skipped++;
                continue;
            }

            var row = new FundPrice { Date = date };
            bool hasAnyPrice = false;

            foreach (var fundName in AllFundNames)
            {
                var priceStr = csv.GetField<string?>(fundName);
                if (string.IsNullOrWhiteSpace(priceStr)) continue;
                if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)) continue;
                if (price > 0)
                {
                    row.SetPrice(fundName, price);
                    hasAnyPrice = true;
                }
            }

            if (hasAnyPrice)
            {
                newRows.Add(row);
            }
        }

        if (newRows.Count > 0)
        {
            _db.FundPrices.AddRange(newRows);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "TSP sync complete: {Total} CSV rows parsed, {Skipped} skipped, {New} new daily price rows inserted.",
                totalRows, skipped, newRows.Count);
        }
        else
        {
            _logger.LogInformation("TSP sync: no new prices to import (already up to date).");
        }
    }

    public async Task<List<FundLatestDto>> GetLatestPricesAsync()
    {
        var recentRows = await _db.FundPrices
            .OrderByDescending(f => f.Date)
            .Take(2)
            .ToListAsync();

        if (recentRows.Count == 0) return [];

        var latestRow = recentRows[0];
        var prevRow = recentRows.Count > 1 ? recentRows[1] : null;

        var result = new List<FundLatestDto>();
        foreach (var fundName in AllFundNames)
        {
            var price = latestRow.GetPrice(fundName);
            if (!price.HasValue) continue;

            var prevPrice = prevRow?.GetPrice(fundName);
            var change = prevPrice.HasValue && prevPrice > 0 ? price.Value - prevPrice.Value : (decimal?)null;
            var changePct = prevPrice.HasValue && prevPrice > 0 ? ((price.Value - prevPrice.Value) / prevPrice.Value) * 100m : (decimal?)null;
            FundDescriptions.TryGetValue(fundName, out var desc);

            result.Add(new FundLatestDto(fundName, price.Value, latestRow.Date, change, changePct, desc));
        }

        return result;
    }

    public async Task<List<FundPriceDto>> GetFundHistoryAsync(string fundName, DateOnly from, DateOnly to)
    {
        var rows = await _db.FundPrices
            .Where(f => f.Date >= from && f.Date <= to)
            .OrderBy(f => f.Date)
            .ToListAsync();

        var result = new List<FundPriceDto>();
        foreach (var row in rows)
        {
            var price = row.GetPrice(fundName);
            if (price.HasValue)
            {
                result.Add(new FundPriceDto(row.Date, fundName, price.Value));
            }
        }
        return result;
    }

    public async Task<Dictionary<string, List<FundPriceDto>>> GetAllFundsHistoryAsync(DateOnly from, DateOnly to)
    {
        var rows = await _db.FundPrices
            .Where(f => f.Date >= from && f.Date <= to)
            .OrderBy(f => f.Date)
            .ToListAsync();

        var result = new Dictionary<string, List<FundPriceDto>>();
        foreach (var fundName in AllFundNames)
        {
            result[fundName] = new List<FundPriceDto>();
        }

        foreach (var row in rows)
        {
            foreach (var fundName in AllFundNames)
            {
                var price = row.GetPrice(fundName);
                if (price.HasValue)
                {
                    result[fundName].Add(new FundPriceDto(row.Date, fundName, price.Value));
                }
            }
        }

        return result;
    }
}
