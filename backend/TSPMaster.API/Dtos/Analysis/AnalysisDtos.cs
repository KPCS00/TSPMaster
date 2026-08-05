namespace TSPMaster.API.Dtos.Analysis;

public record FundScoreDto(
    string FundName,
    double Score,
    string Trend,
    double MomentumScore,
    double VolatilityScore,
    string Recommendation
);

public record AnalysisResultDto(
    int Id,
    DateTime GeneratedAt,
    string PeriodDescription,
    string TopRecommendation,
    string RecommendationText,
    List<FundScoreDto> FundScores,
    string MarketContext
);
