namespace TSPMaster.API.Dtos.Funds;

public record FundPriceDto(
    DateOnly Date,
    string FundName,
    decimal Price
);

public record FundLatestDto(
    string FundName,
    decimal Price,
    DateOnly Date,
    decimal? ChangeAmount,
    decimal? ChangePercent,
    string? Description
);

public record FundHistoryRequest(
    string FundName,
    DateOnly From,
    DateOnly To
);
