namespace TSPMaster.API.Services;

/// <summary>
/// Background service that syncs TSP fund prices nightly from tsp.gov.
/// Runs an immediate sync on startup, then daily at the configured UTC hour.
/// </summary>
public class TspPriceSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TspPriceSyncService> _logger;

    public TspPriceSyncService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<TspPriceSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TSP Price Sync Service starting.");

        // Immediate sync on startup (backfill if DB is empty)
        await RunSyncAsync(stoppingToken);

        // Then run daily at configured UTC hour (default: 02:00 UTC = ~9 PM ET)
        var syncHourUtc = int.TryParse(_config["TspData:SyncHourUtc"], out var h) ? h : 2;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddHours(syncHourUtc);
            if (nextRun <= now) nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogInformation("Next TSP price sync scheduled at {NextRun} UTC (in {Delay:hh\\:mm}).",
                nextRun, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await RunSyncAsync(stoppingToken);
        }

        _logger.LogInformation("TSP Price Sync Service stopped.");
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITspDataService>();
            await service.SyncAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TSP price sync failed.");
        }
    }
}
