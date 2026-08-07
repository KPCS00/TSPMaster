namespace TSPMaster.API.Dtos.Analysis;

public record IntradayMarketQuoteDto(
    string Ticker,
    string FundName,
    decimal OpenPrice,
    decimal CurrentPrice,
    double ChangePercent,
    DateTime FetchedAt
);

public record IntradayMarketSnapshotDto(
    DateTime SnapshotTime,
    List<IntradayMarketQuoteDto> Quotes,
    string MarketSentimentSummary
);
