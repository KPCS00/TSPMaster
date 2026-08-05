using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Models;

namespace TSPMaster.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<FundPrice> FundPrices => Set<FundPrice>();
    public DbSet<FundAllocation> FundAllocations => Set<FundAllocation>();
    public DbSet<PortfolioSnapshot> PortfolioSnapshots => Set<PortfolioSnapshot>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // FundPrice: one row per market date (Primary Key = Date)
        builder.Entity<FundPrice>(entity =>
        {
            entity.HasKey(e => e.Date);

            entity.Property(e => e.GFund).HasPrecision(18, 4);
            entity.Property(e => e.FFund).HasPrecision(18, 4);
            entity.Property(e => e.CFund).HasPrecision(18, 4);
            entity.Property(e => e.SFund).HasPrecision(18, 4);
            entity.Property(e => e.IFund).HasPrecision(18, 4);
            entity.Property(e => e.LIncome).HasPrecision(18, 4);
            entity.Property(e => e.L2030).HasPrecision(18, 4);
            entity.Property(e => e.L2035).HasPrecision(18, 4);
            entity.Property(e => e.L2040).HasPrecision(18, 4);
            entity.Property(e => e.L2045).HasPrecision(18, 4);
            entity.Property(e => e.L2050).HasPrecision(18, 4);
            entity.Property(e => e.L2055).HasPrecision(18, 4);
            entity.Property(e => e.L2060).HasPrecision(18, 4);
            entity.Property(e => e.L2065).HasPrecision(18, 4);
            entity.Property(e => e.L2070).HasPrecision(18, 4);
            entity.Property(e => e.L2075).HasPrecision(18, 4);
        });

        // FundAllocation: user-fund pair uniqueness
        builder.Entity<FundAllocation>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.FundName })
                  .IsUnique()
                  .HasDatabaseName("IX_FundAllocation_User_Fund");

            entity.Property(e => e.Percentage).HasPrecision(5, 2);
            entity.Property(e => e.FundName).HasMaxLength(50);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.FundAllocations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PortfolioSnapshot
        builder.Entity<PortfolioSnapshot>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Date, e.FundName })
                  .IsUnique()
                  .HasDatabaseName("IX_PortfolioSnapshot_User_Date_Fund");

            entity.Property(e => e.Shares).HasPrecision(18, 6);
            entity.Property(e => e.PriceAtDate).HasPrecision(18, 4);
            entity.Property(e => e.Value).HasPrecision(18, 2);
            entity.Property(e => e.FundName).HasMaxLength(50);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.PortfolioSnapshots)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AnalysisResult
        builder.Entity<AnalysisResult>(entity =>
        {
            entity.Property(e => e.TopRecommendation).HasMaxLength(50);
            entity.Property(e => e.PeriodDescription).HasMaxLength(100);
            entity.HasIndex(e => e.IsActive);
        });

        // ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });
    }
}
