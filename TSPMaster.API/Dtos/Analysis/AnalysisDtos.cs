namespace TSPMaster.API.Dtos.Analysis;

public record FundScoreDto(
    string FundName,
    double Score,
    string Trend,
    double MomentumScore,
    double VolatilityScore,
    string Recommendation
);

public record MonthlyMovePlanDto(
    int MoveNumber,
    string Title,
    string TriggerCondition,
    Dictionary<string, decimal> TargetAllocation,
    string Rationale
);

public record ScheduledMoveDto(
    int MoveNumber,
    string DateString,
    int TradingDay,
    Dictionary<string, decimal> TargetAllocation,
    string SeasonalRationale,
    string AiStatusBadge
);

public record DailyCalendarEntryDto(
    string DateString,
    int DayOfMonth,
    int TradingDay,
    string RecommendedFund,
    bool IsMoveDay,
    int? MoveNumber
);

public record AnalysisResultDto(
    int Id,
    DateTime GeneratedAt,
    string PeriodDescription,
    string TopRecommendation,
    string RecommendationText,
    List<FundScoreDto> FundScores,
    string MarketContext,
    string TargetMonth,
    MonthlyMovePlanDto Move1Plan,
    MonthlyMovePlanDto Move2Plan,
    MonthlyMovePlanDto Move3Plan,
    string MacroNewsSummary,
    List<ScheduledMoveDto> ScheduledMoves,
    List<DailyCalendarEntryDto> DailyCalendar,
    string TomorrowEffectiveDate = "",
    Dictionary<string, decimal>? TomorrowAllocation = null,
    IntradayMarketSnapshotDto? IntradayMarketSnapshot = null,
    string HistoricalSeasonalitySummary = ""
);
