namespace TSPMaster.API.Models;

/// <summary>
/// Stores AI-generated investment recommendations from the Gemini analysis engine.
/// </summary>
public class AnalysisResult
{
    public int Id { get; set; }

    /// <summary>UTC timestamp when this analysis was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Analysis period covered (e.g., "6 months").</summary>
    public string PeriodDescription { get; set; } = string.Empty;

    /// <summary>Top recommended fund name (e.g., "C Fund").</summary>
    public string TopRecommendation { get; set; } = string.Empty;

    /// <summary>Full markdown text of the AI recommendation and reasoning.</summary>
    public string RecommendationText { get; set; } = string.Empty;

    /// <summary>JSON-serialized per-fund score objects.</summary>
    public string FundScoresJson { get; set; } = "[]";

    /// <summary>Market sentiment summary used in analysis.</summary>
    public string MarketContext { get; set; } = string.Empty;

    /// <summary>Target calendar month for this strategy (e.g. "August 2026").</summary>
    public string TargetMonth { get; set; } = string.Empty;

    /// <summary>Move 1 Plan (Start of month allocation and reasoning).</summary>
    public string Move1PlanJson { get; set; } = "{}";

    /// <summary>Move 2 Plan (Mid-month tactical adjustment trigger and target allocation).</summary>
    public string Move2PlanJson { get; set; } = "{}";

    /// <summary>Move 3 Plan (Flight-to-safety emergency trigger into 100% G Fund).</summary>
    public string Move3PlanJson { get; set; } = "{}";

    /// <summary>Summary of political, macroeconomic, and financial news drivers.</summary>
    public string MacroNewsSummary { get; set; } = string.Empty;

    /// <summary>JSON-serialized array of specific scheduled moves with exact calendar dates.</summary>
    public string ScheduledMovesJson { get; set; } = "[]";

    /// <summary>JSON-serialized day-by-day fund calendar entries for the current month.</summary>
    public string DailyCalendarJson { get; set; } = "[]";

    /// <summary>Effective date for trade submitted today before 11:00 AM CST (YYYY-MM-DD).</summary>
    public string TomorrowEffectiveDate { get; set; } = string.Empty;

    /// <summary>JSON-serialized dictionary of target fund allocations for tomorrow.</summary>
    public string TomorrowAllocationJson { get; set; } = "{}";

    /// <summary>JSON-serialized live intraday morning market snapshot (Open to 10:30 AM CST).</summary>
    public string IntradayMarketSnapshotJson { get; set; } = "{}";

    /// <summary>Full multi-decade historical seasonality summary for current month & trading day.</summary>
    public string HistoricalSeasonalitySummary { get; set; } = string.Empty;

    /// <summary>Whether this is the current active recommendation.</summary>
    public bool IsActive { get; set; } = true;
}
