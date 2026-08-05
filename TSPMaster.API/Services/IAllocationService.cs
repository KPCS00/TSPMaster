using TSPMaster.API.Dtos.Allocations;

namespace TSPMaster.API.Services;

public interface IAllocationService
{
    Task<List<AllocationDto>> GetAllocationsAsync(string userId);
    Task SetAllocationsAsync(string userId, List<AllocationItem> allocations);
}
