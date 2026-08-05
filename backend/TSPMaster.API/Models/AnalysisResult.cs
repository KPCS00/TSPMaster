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

    /// <summary>Whether this is the current active recommendation.</summary>
    public bool IsActive { get; set; } = true;
}
