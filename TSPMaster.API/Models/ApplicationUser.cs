using Microsoft.AspNetCore.Identity;

namespace TSPMaster.API.Models;

/// <summary>
/// Extends IdentityUser with TSP-specific profile fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? GoogleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Number of interfund transfers executed by user in the current calendar month.</summary>
    public int MonthlyTransfersCount { get; set; } = 0;

    /// <summary>Calendar month string (e.g. "2026-08") for which MonthlyTransfersCount applies.</summary>
    public string LastTransferMonth { get; set; } = string.Empty;

    // Navigation
    public ICollection<FundAllocation> FundAllocations { get; set; } = new List<FundAllocation>();
    public ICollection<PortfolioSnapshot> PortfolioSnapshots { get; set; } = new List<PortfolioSnapshot>();
}
