using TSPMaster.API.Dtos.Funds;

namespace TSPMaster.API.Services;

public interface ITspDataService
{
    /// <summary>Fetch and upsert TSP fund prices from tsp.gov CSV.</summary>
    Task SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the latest price for each fund.</summary>
    Task<List<FundLatestDto>> GetLatestPricesAsync();

    /// <summary>Returns price history for a specific fund between two dates.</summary>
    Task<List<FundPriceDto>> GetFundHistoryAsync(string fundName, DateOnly from, DateOnly to);

    /// <summary>Returns price history for all funds between two dates.</summary>
    Task<Dictionary<string, List<FundPriceDto>>> GetAllFundsHistoryAsync(DateOnly from, DateOnly to);

    /// <summary>Returns list of all known fund names.</summary>
    IReadOnlyList<string> GetAllFundNames();
}
