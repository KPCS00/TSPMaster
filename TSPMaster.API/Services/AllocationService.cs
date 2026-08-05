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

    public async Task SetAllocationsAsync(string userId, List<AllocationItem> allocations)
    {
        // Validate: must sum to 100%
        var total = allocations.Sum(a => a.Percentage);
        if (Math.Abs(total - 100m) > 0.01m)
            throw new InvalidOperationException($"Allocations must sum to 100%. Current total: {total}%");

        // Remove all existing allocations for this user
        var existing = await _db.FundAllocations
            .Where(a => a.UserId == userId)
            .ToListAsync();
        _db.FundAllocations.RemoveRange(existing);

        // Add new ones (only non-zero)
        var newAllocations = allocations
            .Where(a => a.Percentage > 0)
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
