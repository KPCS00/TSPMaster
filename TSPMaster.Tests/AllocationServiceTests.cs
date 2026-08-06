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
    public async Task SetAllocations_ShouldThrow_WhenTotalPercentageIsNot100()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test@example.com", Email = "test@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);
        var invalidAllocations = new List<AllocationItem>
        {
            new("C Fund", 50m),
            new("S Fund", 40m) // Total = 90%
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetAllocationsAsync(user.Id, invalidAllocations));

        Assert.Contains("must sum to 100%", ex.Message);
    }

    [Fact]
    public async Task SetAllocations_ShouldSucceed_WhenValid()
    {
        // Arrange
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

        // Act
        await service.SetAllocationsAsync(user.Id, validAllocations);

        // Assert
        var updatedAllocations = await service.GetAllocationsAsync(user.Id);
        Assert.Equal(2, updatedAllocations.Count);
        Assert.Contains(updatedAllocations, a => a.FundName == "C Fund" && a.Percentage == 60m);
        Assert.Contains(updatedAllocations, a => a.FundName == "S Fund" && a.Percentage == 40m);
    }

    [Fact]
    public async Task SetAllocations_ShouldEnforce100PercentGFund_OnThirdMove()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "test@example.com",
            Email = "test@example.com",
            MonthlyTransfersCount = 2, // 2 moves used, next is 3rd
            LastTransferMonth = currentMonth
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);

        var invalidThirdMove = new List<AllocationItem>
        {
            new("C Fund", 100m)
        };

        var validThirdMove = new List<AllocationItem>
        {
            new("G Fund", 100m)
        };

        // Act & Assert invalid third move
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetAllocationsAsync(user.Id, invalidThirdMove));
        Assert.Contains("3rd Interfund Transfer of the month must be 100% G Fund", ex.Message);

        // Act & Assert valid third move
        await service.SetAllocationsAsync(user.Id, validThirdMove);
        var allocations = await service.GetAllocationsAsync(user.Id);
        Assert.Single(allocations);
        Assert.Equal("G Fund", allocations[0].FundName);
        Assert.Equal(100m, allocations[0].Percentage);
    }

    [Fact]
    public async Task SetAllocations_ShouldReject_WhenAllThreeTransfersUsed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "test@example.com",
            Email = "test@example.com",
            MonthlyTransfersCount = 3, // All 3 moves used
            LastTransferMonth = currentMonth
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AllocationService(db);

        var allocation = new List<AllocationItem>
        {
            new("G Fund", 100m)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetAllocationsAsync(user.Id, allocation));
        Assert.Contains("You have used all 3 Interfund Transfers", ex.Message);
    }
}
