using System.Text.Json;
using TSPMaster.API.Dtos.Analysis;

namespace TSPMaster.API.Services;

public class IntradayMarketService : IIntradayMarketService
{
    private static readonly (string Ticker, string FundName)[] Proxies =
    [
        ("SPY", "C Fund"),
        ("VXF", "S Fund"),
        ("EFA", "I Fund"),
        ("AGG", "F Fund"),
        ("BIL", "G Fund")
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IntradayMarketService> _logger;

    public IntradayMarketService(
        IHttpClientFactory httpClientFactory,
        ILogger<IntradayMarketService> _logger)
    {
        _httpClientFactory = httpClientFactory;
        this._logger = _logger;
    }

    public async Task<IntradayMarketSnapshotDto> GetMorningMarketSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("MarketDataClient");
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        var quotes = new List<IntradayMarketQuoteDto>();
        var now = DateTime.UtcNow;

        foreach (var (ticker, fundName) in Proxies)
        {
            try
            {
                var quote = await FetchProxyQuoteAsync(client, ticker, fundName, cancellationToken);
                quotes.Add(quote);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch intraday quote for {Ticker} ({FundName}). Using baseline default.", ticker, fundName);
                quotes.Add(new IntradayMarketQuoteDto(
                    ticker,
                    fundName,
                    100.00m,
                    100.00m,
                    0.0,
                    now
                ));
            }
        }

        // Determine market sentiment from S&P 500 (SPY / C Fund) & Small Cap (VXF / S Fund)
        var spyChange = quotes.FirstOrDefault(q => q.Ticker == "SPY")?.ChangePercent ?? 0.0;
        var vxfChange = quotes.FirstOrDefault(q => q.Ticker == "VXF")?.ChangePercent ?? 0.0;

        string sentiment = (spyChange, vxfChange) switch
        {
            ( > 0.4, > 0.4) => "Strong Morning Rally (Bullish Risk-On)",
            ( > 0.0, > 0.0) => "Moderate Morning Gains (Positive Bias)",
            ( < -0.4, < -0.4) => "Morning Sell-Off (Bearish Flight-to-Safety)",
            ( < 0.0, < 0.0) => "Mild Morning Weakness (Cautionary)",
            _ => "Mixed Intraday Trading (Divergent Fund Momentum)"
        };

        var summary = $"Live Morning Market Snapshot (Open to 10:30 AM CST): {sentiment}. SPY (C Fund Proxy): {spyChange:+0.00;-0.00}%, VXF (S Fund Proxy): {vxfChange:+0.00;-0.00}%.";

        return new IntradayMarketSnapshotDto(now, quotes, summary);
    }

    private static async Task<IntradayMarketQuoteDto> FetchProxyQuoteAsync(
        HttpClient client,
        string ticker,
        string fundName,
        CancellationToken cancellationToken)
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1m&range=1d";
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        var result = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0];

        var meta = result.GetProperty("meta");

        decimal regularMarketPrice = meta.TryGetProperty("regularMarketPrice", out var rmp)
            ? rmp.GetDecimal()
            : 0m;

        decimal chartPreviousClose = meta.TryGetProperty("chartPreviousClose", out var cpc)
            ? cpc.GetDecimal()
            : (meta.TryGetProperty("previousClose", out var pc) ? pc.GetDecimal() : regularMarketPrice);

        // Try getting opening price from indicators or chartPreviousClose
        decimal openPrice = chartPreviousClose;
        if (result.TryGetProperty("indicators", out var indicators) &&
            indicators.TryGetProperty("quote", out var quoteArr) &&
            quoteArr.GetArrayLength() > 0 &&
            quoteArr[0].TryGetProperty("open", out var openArr) &&
            openArr.GetArrayLength() > 0)
        {
            foreach (var el in openArr.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var validOpen) && validOpen > 0)
                {
                    openPrice = validOpen;
                    break;
                }
            }
        }

        if (openPrice <= 0) openPrice = regularMarketPrice > 0 ? regularMarketPrice : 100m;
        if (regularMarketPrice <= 0) regularMarketPrice = openPrice;

        double changePercent = (double)((regularMarketPrice - openPrice) / openPrice * 100m);

        return new IntradayMarketQuoteDto(
            ticker,
            fundName,
            Math.Round(openPrice, 2),
            Math.Round(regularMarketPrice, 2),
            Math.Round(changePercent, 2),
            DateTime.UtcNow
        );
    }
}
