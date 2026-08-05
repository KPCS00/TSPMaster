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
