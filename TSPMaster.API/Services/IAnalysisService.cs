using TSPMaster.API.Dtos.Analysis;

namespace TSPMaster.API.Services;

public interface IAnalysisService
{
    Task<AnalysisResultDto?> GetLatestRecommendationAsync();
    Task<AnalysisResultDto> RefreshRecommendationAsync();
}
