using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Models;

namespace TSPMaster.API.Data;

/// <summary>
/// Seeds the database on startup: applies pending migrations and creates default roles.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            // Apply any pending EF Core migrations
            var pending = await context.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                logger.LogInformation("Applying {Count} pending migration(s)...", pending.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }

            // Seed roles
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created role: {Role}", role);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database initialization.");
            throw;
        }
    }
}
