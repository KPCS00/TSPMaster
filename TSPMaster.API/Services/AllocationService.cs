using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Allocations;
using TSPMaster.API.Models;

namespace TSPMaster.API.Services;

public class AllocationService : IAllocationService
{
    private readonly ApplicationDbContext _db;

    public AllocationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AllocationDto>> GetAllocationsAsync(string userId)
    {
        return await _db.FundAllocations
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.FundName)
            .Select(a => new AllocationDto(a.FundName, a.Percentage, a.UpdatedAt))
            .ToListAsync();
    }

    public async Task<TransferStatusDto> GetTransferStatusAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var count = user.LastTransferMonth == currentMonth ? user.MonthlyTransfersCount : 0;
        var remaining = Math.Max(0, 3 - count);
        var isMove3GFundOnly = count == 2;

        return new TransferStatusDto(count, remaining, 3, isMove3GFundOnly, currentMonth);
    }

    public async Task SetAllocationsAsync(string userId, List<AllocationItem> allocations)
    {
        // Validate: must sum to 100%
        var total = allocations.Sum(a => a.Percentage);
        if (Math.Abs(total - 100m) > 0.01m)
            throw new InvalidOperationException($"Allocations must sum to 100%. Current total: {total}%");

        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        if (user.LastTransferMonth != currentMonth)
        {
            user.LastTransferMonth = currentMonth;
            user.MonthlyTransfersCount = 0;
        }

        if (user.MonthlyTransfersCount >= 3)
        {
            throw new InvalidOperationException("You have used all 3 Interfund Transfers (IFT) allowed for this calendar month.");
        }

        var activeAllocations = allocations.Where(a => a.Percentage > 0).ToList();

        // 3rd Move Rule: Restricted to 100% G Fund
        if (user.MonthlyTransfersCount == 2)
        {
            bool isAllGFund = activeAllocations.Count == 1 &&
                              activeAllocations[0].FundName.Equals("G Fund", StringComparison.OrdinalIgnoreCase) &&
                              activeAllocations[0].Percentage == 100m;

            if (!isAllGFund)
            {
                throw new InvalidOperationException("Under TSP rules, your 3rd Interfund Transfer of the month must be 100% G Fund.");
            }
        }

        // Increment monthly transfer count
        user.MonthlyTransfersCount++;
        _db.Users.Update(user);

        // Remove all existing allocations for this user
        var existing = await _db.FundAllocations
            .Where(a => a.UserId == userId)
            .ToListAsync();
        _db.FundAllocations.RemoveRange(existing);

        // Add new ones (only non-zero)
        var newAllocations = activeAllocations
            .Select(a => new FundAllocation
            {
                UserId = userId,
                FundName = a.FundName,
                Percentage = a.Percentage,
                UpdatedAt = DateTime.UtcNow
            });

        _db.FundAllocations.AddRange(newAllocations);
        await _db.SaveChangesAsync();
    }
}
