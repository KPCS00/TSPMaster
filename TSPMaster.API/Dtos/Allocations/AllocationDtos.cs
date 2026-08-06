namespace TSPMaster.API.Dtos.Allocations;

public record AllocationDto(
    string FundName,
    decimal Percentage,
    DateTime UpdatedAt
);

public record AllocationRequest(
    List<AllocationItem> Allocations
);

public record AllocationItem(
    string FundName,
    decimal Percentage
);

public record TransferStatusDto(
    int TransfersUsed,
    int RemainingTransfers,
    int MaxTransfers,
    bool IsMove3GFundOnly,
    string CurrentMonth
);

public record AllocationMoveDto(
    int Id,
    DateTime EffectiveDate,
    string Description,
    decimal BalanceAtMove,
    List<AllocationItem> Allocations,
    int MoveNumberInMonth,
    string MonthKey,
    DateTime CreatedAt
);

public record SetBalanceRequest(
    decimal Balance,
    DateTime? EffectiveDate
);

public record RecordMoveRequest(
    DateTime EffectiveDate,
    string? Description,
    List<AllocationItem> Allocations,
    decimal? UpdatedBalance
);

public record AllocationOverviewDto(
    decimal InitialTspBalance,
    decimal CurrentTspBalance,
    DateTime? InitialBalanceDate,
    List<AllocationDto> CurrentAllocations,
    TransferStatusDto TransferStatus,
    List<AllocationMoveDto> MoveHistory,
    string? RecommendedFund,
    string? RecommendationText
);
