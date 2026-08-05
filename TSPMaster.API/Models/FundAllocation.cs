namespace TSPMaster.API.Models;

/// <summary>
/// Stores a user's TSP contribution allocation for a specific fund (percentage).
/// All allocations for a user must sum to 100%.
/// </summary>
public class FundAllocation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FundName { get; set; } = string.Empty;

    /// <summary>Percentage allocated (0–100). All user allocations must sum to 100.</summary>
    public decimal Percentage { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
