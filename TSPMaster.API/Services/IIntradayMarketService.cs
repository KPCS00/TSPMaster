using TSPMaster.API.Dtos.Analysis;

namespace TSPMaster.API.Services;

public interface IIntradayMarketService
{
    /// <summary>
    /// Fetches live morning market quotes (Open to 10:30 AM CST) for TSP fund benchmark proxies (SPY, VXF, EFA, AGG, BIL).
    /// </summary>
    Task<IntradayMarketSnapshotDto> GetMorningMarketSnapshotAsync(CancellationToken cancellationToken = default);
}
