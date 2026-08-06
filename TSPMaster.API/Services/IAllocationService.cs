using TSPMaster.API.Dtos.Allocations;

namespace TSPMaster.API.Services;

public interface IAllocationService
{
    Task<List<AllocationDto>> GetAllocationsAsync(string userId);
    Task SetAllocationsAsync(string userId, List<AllocationItem> allocations);
    Task<TransferStatusDto> GetTransferStatusAsync(string userId);
    Task<AllocationOverviewDto> GetOverviewAsync(string userId);
    Task SetInitialBalanceAsync(string userId, decimal balance, DateTime? effectiveDate);
    Task<AllocationMoveDto> RecordMoveAsync(string userId, RecordMoveRequest request);
    Task DeleteMoveAsync(string userId, int moveId);
    Task<List<AllocationMoveDto>> GetMoveHistoryAsync(string userId);
}
