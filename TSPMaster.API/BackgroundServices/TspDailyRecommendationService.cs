using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Data;
using TSPMaster.API.Helpers;

namespace TSPMaster.API.Services;

/// <summary>
/// Background service that runs daily at 10:30 AM CST (30 minutes prior to 11:00 AM CST TSP.gov trade cutoff).
/// Re-evaluates monthly strategy against changing market news & indicators and emails daily recommendations.
/// </summary>
public class TspDailyRecommendationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TspDailyRecommendationService> _logger;

    public TspDailyRecommendationService(
        IServiceScopeFactory scopeFactory,
        ILogger<TspDailyRecommendationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TSP Daily 10:30 AM CST Recommendation Service starting.");

        var centralZone = GetCentralTimeZone();

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var nowCentral = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, centralZone);

            var nextRunCentral = new DateTime(nowCentral.Year, nowCentral.Month, nowCentral.Day, 10, 30, 0, DateTimeKind.Unspecified);
            if (nowCentral >= nextRunCentral)
            {
                nextRunCentral = nextRunCentral.AddDays(1);
            }

            var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunCentral, centralZone);
            var delay = nextRunUtc - nowUtc;

            _logger.LogInformation("Next 10:30 AM CST Strategy Review scheduled for {NextRunCentral} Central Time (in {Delay:hh\\:mm\\:ss}).",
                nextRunCentral.ToString("yyyy-MM-dd HH:mm:ss"), delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await ProcessDailyRecommendationsAsync(stoppingToken);
        }

        _logger.LogInformation("TSP Daily 10:30 AM CST Recommendation Service stopped.");
    }

    public async Task ProcessDailyRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting daily 10:30 AM CST strategy review and user email notifications...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var analysisService = scope.ServiceProvider.GetRequiredService<IAnalysisService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var allocationService = scope.ServiceProvider.GetRequiredService<IAllocationService>();

            // Assess current strategy / refresh AI metrics
            var rec = await analysisService.GetLatestRecommendationAsync()
                ?? await analysisService.RefreshRecommendationAsync();

            var users = await db.Users.Where(u => !string.IsNullOrEmpty(u.Email)).ToListAsync(cancellationToken);
            _logger.LogInformation("Sending daily 10:30 AM CST recommendations to {Count} user(s).", users.Count);

            foreach (var user in users)
            {
                try
                {
                    var status = await allocationService.GetTransferStatusAsync(user.Id);

                    string actionAdvice = status.RemainingTransfers switch
                    {
                        0 => "No transfers remaining for this month. Hold current allocation.",
                        1 => $"Move 3 (Emergency Exit) active: Restrict transfer to 100% G Fund if safety trigger is met.",
                        _ => $"Move 1/2 Strategy Active: Primary seasonal & macro recommendation is top performer {rec.TopRecommendation}."
                    };

                    await emailService.SendDailyRecommendationEmailAsync(
                        user.Email!,
                        string.IsNullOrWhiteSpace(user.FirstName) ? "TSP Investor" : user.FirstName,
                        rec.RecommendationText,
                        actionAdvice,
                        status.RemainingTransfers,
                        rec.TargetMonth ?? status.CurrentMonth
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send 10:30 AM CST recommendation email to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily 10:30 AM CST strategy review failed.");
        }
    }

    private static TimeZoneInfo GetCentralTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
