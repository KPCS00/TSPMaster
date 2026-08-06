using Microsoft.AspNetCore.Identity;

namespace TSPMaster.API.Models;

/// <summary>
/// Extends IdentityUser with TSP-specific profile fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Number of interfund transfers executed by user in the current calendar month.</summary>
    public int MonthlyTransfersCount { get; set; } = 0;

    /// <summary>Calendar month string (e.g. "2026-08") for which MonthlyTransfersCount applies.</summary>
    public string LastTransferMonth { get; set; } = string.Empty;

    /// <summary>Initial TSP balance entered by user when setting up tracking.</summary>
    public decimal InitialTspBalance { get; set; } = 0m;

    /// <summary>Date on which initial TSP balance was recorded.</summary>
    public DateTime? InitialBalanceDate { get; set; }

    /// <summary>Current total TSP balance ($).</summary>
    public decimal CurrentTspBalance { get; set; } = 0m;

    // Navigation
    public ICollection<FundAllocation> FundAllocations { get; set; } = new List<FundAllocation>();
    public ICollection<PortfolioSnapshot> PortfolioSnapshots { get; set; } = new List<PortfolioSnapshot>();
    public ICollection<AllocationMove> AllocationMoves { get; set; } = new List<AllocationMove>();
}
