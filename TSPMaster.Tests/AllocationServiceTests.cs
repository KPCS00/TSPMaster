using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Allocations;
using TSPMaster.API.Models;
using TSPMaster.API.Services;
using Xunit;

namespace TSPMaster.Tests;

public class AllocationServiceTests
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
    public async Task SetInitialBalance_ShouldUpdateUserBalance()
    {
        using var db = CreateInMemoryDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test@example.com", Email = "test@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);
        await service.SetInitialBalanceAsync(user.Id, 75000m, DateTime.UtcNow);

        var overview = await service.GetOverviewAsync(user.Id);
        Assert.Equal(75000m, overview.InitialTspBalance);
        Assert.Equal(75000m, overview.CurrentTspBalance);
    }

    [Fact]
    public async Task RecordMove_ShouldThrow_WhenTotalPercentageIsNot100()
    {
        using var db = CreateInMemoryDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test@example.com", Email = "test@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);
        var invalidAllocations = new List<AllocationItem>
        {
            new("C Fund", 50m),
            new("S Fund", 40m)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordMoveAsync(user.Id, new RecordMoveRequest(DateTime.UtcNow, "Test", invalidAllocations, null)));

        Assert.Contains("must sum to 100%", ex.Message);
    }

    [Fact]
    public async Task RecordMove_ShouldSucceed_AndSaveToHistory()
    {
        using var db = CreateInMemoryDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test@example.com", Email = "test@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);
        var validAllocations = new List<AllocationItem>
        {
            new("C Fund", 60m),
            new("S Fund", 40m)
        };

        var move = await service.RecordMoveAsync(user.Id, new RecordMoveRequest(
            DateTime.UtcNow, "Initial Rebalance", validAllocations, 50000m
        ));

        Assert.NotNull(move);
        Assert.Equal(1, move.MoveNumberInMonth);
        Assert.Equal(50000m, move.BalanceAtMove);

        var history = await service.GetMoveHistoryAsync(user.Id);
        Assert.Single(history);
        Assert.Equal("Initial Rebalance", history[0].Description);
    }

    [Fact]
    public async Task RecordMove_ShouldEnforce100PercentGFund_OnThirdMove()
    {
        using var db = CreateInMemoryDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test@example.com", Email = "test@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);
        var now = DateTime.UtcNow;

        // Move 1
        await service.RecordMoveAsync(user.Id, new RecordMoveRequest(now, "Move 1", new List<AllocationItem> { new("C Fund", 100m) }, null));
        // Move 2
        await service.RecordMoveAsync(user.Id, new RecordMoveRequest(now, "Move 2", new List<AllocationItem> { new("S Fund", 100m) }, null));

        // Invalid Move 3 (non G-fund)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordMoveAsync(user.Id, new RecordMoveRequest(now, "Move 3 invalid", new List<AllocationItem> { new("C Fund", 100m) }, null)));
        Assert.Contains("3rd Interfund Transfer of the month must be 100% G Fund", ex.Message);

        // Valid Move 3 (100% G Fund)
        var validMove3 = await service.RecordMoveAsync(user.Id, new RecordMoveRequest(now, "Move 3 valid", new List<AllocationItem> { new("G Fund", 100m) }, null));
        Assert.Equal(3, validMove3.MoveNumberInMonth);

        // Attempt Move 4 (should be rejected)
        var ex4 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordMoveAsync(user.Id, new RecordMoveRequest(now, "Move 4 blocked", new List<AllocationItem> { new("G Fund", 100m) }, null)));
        Assert.Contains("used all 3 allowed Interfund Transfers", ex4.Message);
    }

    [Fact]
    public async Task DeleteMove_ShouldRemoveMoveAndReindex()
    {
        using var db = CreateInMemoryDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test@example.com", Email = "test@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);
        var now = DateTime.UtcNow;

        var move1 = await service.RecordMoveAsync(user.Id, new RecordMoveRequest(now.AddDays(-2), "Move 1", new List<AllocationItem> { new("C Fund", 100m) }, null));
        var move2 = await service.RecordMoveAsync(user.Id, new RecordMoveRequest(now.AddDays(-1), "Move 2", new List<AllocationItem> { new("S Fund", 100m) }, null));

        var historyBefore = await service.GetMoveHistoryAsync(user.Id);
        Assert.Equal(2, historyBefore.Count);

        await service.DeleteMoveAsync(user.Id, move1.Id);

        var historyAfter = await service.GetMoveHistoryAsync(user.Id);
        Assert.Single(historyAfter);
        Assert.Equal("Move 2", historyAfter[0].Description);
        Assert.Equal(1, historyAfter[0].MoveNumberInMonth);
    }
}
