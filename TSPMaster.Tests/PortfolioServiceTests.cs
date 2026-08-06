using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Allocations;
using TSPMaster.API.Models;
using TSPMaster.API.Services;
using Xunit;

namespace TSPMaster.Tests;

public class PortfolioServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task GetPerformanceSummary_ShouldCalculatePerformanceWithMoveHistory()
    {
        using var db = CreateInMemoryDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "test@example.com",
            Email = "test@example.com",
            InitialTspBalance = 100000m,
            CurrentTspBalance = 100000m
        };
        db.Users.Add(user);

        // Add 3 days of fund prices
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var day1 = today.AddDays(-2);
        var day2 = today.AddDays(-1);
        var day3 = today;

        db.FundPrices.AddRange(
            new FundPrice { Date = day1, GFund = 10m, CFund = 50m, SFund = 20m },
            new FundPrice { Date = day2, GFund = 10.1m, CFund = 55m, SFund = 22m },
            new FundPrice { Date = day3, GFund = 10.2m, CFund = 60m, SFund = 24m }
        );
        await db.SaveChangesAsync();

        var allocService = new AllocationService(db);
        var portfolioService = new PortfolioService(db);

        // Record Move 1 on day 1: 100% C Fund
        await allocService.RecordMoveAsync(user.Id, new RecordMoveRequest(
            day1.ToDateTime(TimeOnly.MinValue), "Move 1", new List<AllocationItem> { new("C Fund", 100m) }, 100000m
        ));

        // Get performance summary for 7 days
        var summary = await portfolioService.GetPerformanceSummaryAsync(user.Id, 7);

        Assert.NotNull(summary);
        Assert.True(summary.History.Count >= 3);

        // Day 1 value should be ~$100,000
        // Day 3 value should reflect C Fund rising from 50 to 60 (+20%) = ~$120,000
        var lastPoint = summary.History.Last();
        Assert.Equal(120000m, Math.Round(lastPoint.PortfolioValue, 2));
        Assert.Equal(20000m, Math.Round(summary.TotalGain, 2));
        Assert.Equal(20m, Math.Round(summary.TotalGainPercent, 2));
    }
}
