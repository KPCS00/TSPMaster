using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TSPMaster.API.Data;

/// <summary>
/// Design-time factory used by EF Core tools (migrations, database update).
/// This bypasses the Program.cs startup and directly constructs the DbContext,
/// avoiding issues with EnableRetryOnFailure and hosted services during tooling.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Load config from appsettings.json so we pick up the real connection string
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string not found in appsettings.json");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            // Do NOT use EnableRetryOnFailure during design-time — it causes
            // the EF tools to fail with "Named Pipes Provider" fallback errors.
            sqlOptions.CommandTimeout(60);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
