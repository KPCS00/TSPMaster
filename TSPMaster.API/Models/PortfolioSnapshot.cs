namespace TSPMaster.API.Models;

/// <summary>
/// Daily snapshot of a user's computed portfolio value per fund.
/// Used to track historical performance against fund benchmarks.
/// </summary>
public class PortfolioSnapshot
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string FundName { get; set; } = string.Empty;

    /// <summary>Number of shares the user holds in this fund.</summary>
    public decimal Shares { get; set; }

    /// <summary>Closing price on this date (denormalized for performance).</summary>
    public decimal PriceAtDate { get; set; }

    /// <summary>Total value = Shares * PriceAtDate.</summary>
    public decimal Value { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
