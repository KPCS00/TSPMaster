namespace TSPMaster.API.Dtos.Users;

public record UserProfileDto(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record PerformanceDataPointDto(
    DateOnly Date,
    decimal PortfolioValue,
    Dictionary<string, decimal> FundValues
);

public record PerformanceSummaryDto(
    decimal TotalValue,
    decimal TotalGain,
    decimal TotalGainPercent,
    List<PerformanceDataPointDto> History
);
