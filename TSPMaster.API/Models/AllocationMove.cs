namespace TSPMaster.API.Models;

/// <summary>
/// Historical record of an Interfund Transfer (IFT) move made by a user on tsp.gov.
/// </summary>
public class AllocationMove
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Date the move was executed / effective on tsp.gov.</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>User notes or description for this move (e.g., "Followed AI Recommendation").</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Recorded total TSP balance ($) at the time of move execution.</summary>
    public decimal BalanceAtMove { get; set; }

    /// <summary>JSON string representation of target allocations list [{ "FundName": "C Fund", "Percentage": 50 }].</summary>
    public string AllocationsJson { get; set; } = string.Empty;

    /// <summary>Move number in the calendar month (1, 2, or 3).</summary>
    public int MoveNumberInMonth { get; set; }

    /// <summary>Calendar month string (e.g. "2026-08").</summary>
    public string MonthKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
