using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Allocations;
using TSPMaster.API.Dtos.Users;
using TSPMaster.API.Models;

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

        var user = await _db.Users.FindAsync(userId);
        var baseBalance = user?.InitialTspBalance > 0
            ? user.InitialTspBalance
            : (user?.CurrentTspBalance > 0 ? user.CurrentTspBalance : 10_000m);

        // Fetch user's move history ordered by date
        var moves = await _db.AllocationMoves
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.EffectiveDate)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync();

        // Current fund allocations as fallback
        var currentAllocations = await _db.FundAllocations
            .Where(a => a.UserId == userId)
            .ToListAsync();

        // Get daily fund prices for date range
        var prices = await _db.FundPrices
            .Where(f => f.Date >= from && f.Date <= to)
            .OrderBy(f => f.Date)
            .ToListAsync();

        if (!prices.Any())
        {
            return new PerformanceSummaryDto(baseBalance, 0, 0, []);
        }

        // Helper to get active allocation list for a given date
        List<AllocationItem> GetAllocationForDate(DateOnly date)
        {
            var dateDt = date.ToDateTime(TimeOnly.MinValue);
            var activeMove = moves.LastOrDefault(m => m.EffectiveDate.Date <= dateDt.Date);

            if (activeMove != null && !string.IsNullOrEmpty(activeMove.AllocationsJson))
            {
                var parsed = JsonSerializer.Deserialize<List<AllocationItem>>(activeMove.AllocationsJson);
                if (parsed != null && parsed.Any()) return parsed;
            }

            if (currentAllocations.Any())
            {
                return currentAllocations.Select(a => new AllocationItem(a.FundName, a.Percentage)).ToList();
            }

            // Default fallback: 100% G Fund
            return new List<AllocationItem> { new("G Fund", 100m) };
        }

        var history = new List<PerformanceDataPointDto>();

        // Rebalancing simulation
        var currentShares = new Dictionary<string, decimal>();
        List<AllocationItem>? lastAllocation = null;

        foreach (var row in prices)
        {
            var activeAlloc = GetAllocationForDate(row.Date);

            // Rebalance if initial day or if allocation changed on this date
            bool shouldRebalance = (lastAllocation == null) ||
                                  !AreAllocationsEqual(lastAllocation, activeAlloc);

            if (shouldRebalance)
            {
                decimal portfolioValueAtRebalance = 0m;
                if (lastAllocation == null)
                {
                    portfolioValueAtRebalance = baseBalance;
                }
                else
                {
                    // Compute current market value of existing shares
                    foreach (var kvp in currentShares)
                    {
                        var p = row.GetPrice(kvp.Key) ?? 0m;
                        portfolioValueAtRebalance += kvp.Value * p;
                    }
                    if (portfolioValueAtRebalance <= 0) portfolioValueAtRebalance = baseBalance;
                }

                // Re-calculate shares per target percentage
                currentShares.Clear();
                foreach (var item in activeAlloc)
                {
                    var p = row.GetPrice(item.FundName) ?? 0m;
                    if (p > 0)
                    {
                        var fundAllocVal = portfolioValueAtRebalance * (item.Percentage / 100m);
                        currentShares[item.FundName] = fundAllocVal / p;
                    }
                }
                lastAllocation = activeAlloc;
            }

            // Calculate portfolio value for the day
            decimal totalValue = 0m;
            var fundValues = new Dictionary<string, decimal>();
            foreach (var kvp in currentShares)
            {
                var p = row.GetPrice(kvp.Key) ?? 0m;
                var val = kvp.Value * p;
                fundValues[kvp.Key] = val;
                totalValue += val;
            }

            history.Add(new PerformanceDataPointDto(row.Date, totalValue, fundValues));
        }

        var currentValue = history.LastOrDefault()?.PortfolioValue ?? baseBalance;
        var gain = currentValue - baseBalance;
        var gainPct = baseBalance > 0 ? (gain / baseBalance) * 100m : 0m;

        return new PerformanceSummaryDto(currentValue, gain, gainPct, history);
    }

    private static bool AreAllocationsEqual(List<AllocationItem> a, List<AllocationItem> b)
    {
        if (a.Count != b.Count) return false;
        var dictA = a.ToDictionary(x => x.FundName, x => x.Percentage);
        foreach (var item in b)
        {
            if (!dictA.TryGetValue(item.FundName, out var pct) || pct != item.Percentage)
                return false;
        }
        return true;
    }
}
