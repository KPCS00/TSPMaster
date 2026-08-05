using TSPMaster.API.Dtos.Users;

namespace TSPMaster.API.Services;

public interface IPortfolioService
{
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(string userId, int days = 90);
}
