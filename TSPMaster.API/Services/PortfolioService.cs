using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Users;

namespace TSPMaster.API.Services;

public class PortfolioService : IPortfolioService
{
    private readonly ApplicationDbContext _db;

    public PortfolioService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(string userId, int days = 90)
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get user's current allocations
        var allocations = await _db.FundAllocations
            .Where(a => a.UserId == userId)
            .ToListAsync();

        if (!allocations.Any())
        {
            return new PerformanceSummaryDto(0, 0, 0, []);
        }

        // Get daily wide fund prices for the date range
        var prices = await _db.FundPrices
            .Where(f => f.Date >= from && f.Date <= to)
            .OrderBy(f => f.Date)
            .ToListAsync();

        if (!prices.Any())
        {
            return new PerformanceSummaryDto(0, 0, 0, []);
        }

        // Find starting prices (first date row)
        var firstRow = prices.First();

        // Compute baseline portfolio value (normalized: assume $10,000 starting value)
        const decimal baseValue = 10_000m;
        var startingShares = new Dictionary<string, decimal>();
        foreach (var alloc in allocations)
        {
            var fundStartPrice = firstRow.GetPrice(alloc.FundName) ?? 0m;
            if (fundStartPrice > 0)
            {
                var fundValue = baseValue * (alloc.Percentage / 100m);
                startingShares[alloc.FundName] = fundValue / fundStartPrice;
            }
        }

        var history = new List<PerformanceDataPointDto>();
        foreach (var row in prices)
        {
            var fundValues = new Dictionary<string, decimal>();
            decimal totalValue = 0m;

            foreach (var alloc in allocations)
            {
                var price = row.GetPrice(alloc.FundName) ?? 0m;
                if (startingShares.TryGetValue(alloc.FundName, out var shares) && price > 0)
                {
                    var val = shares * price;
                    fundValues[alloc.FundName] = val;
                    totalValue += val;
                }
            }

            history.Add(new PerformanceDataPointDto(row.Date, totalValue, fundValues));
        }

        var currentValue = history.LastOrDefault()?.PortfolioValue ?? 0m;
        var gain = currentValue - baseValue;
        var gainPct = baseValue > 0 ? (gain / baseValue) * 100m : 0m;

        return new PerformanceSummaryDto(currentValue, gain, gainPct, history);
    }
}
