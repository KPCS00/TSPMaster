using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSPMaster.API.Dtos.Analysis;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly ILogger<AnalysisController> _logger;

    // Simple throttle: prevent back-to-back refreshes
    private static DateTime _lastRefreshAt = DateTime.MinValue;
    private static readonly TimeSpan RefreshCooldown = TimeSpan.FromMinutes(30);

    public AnalysisController(IAnalysisService analysisService, ILogger<AnalysisController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    /// <summary>Get the latest cached AI investment recommendation.</summary>
    [HttpGet("recommendation")]
    [ProducesResponseType(typeof(AnalysisResultDto), 200)]
    [ProducesResponseType(204)]
    public async Task<ActionResult<AnalysisResultDto>> GetRecommendation()
    {
        var result = await _analysisService.GetLatestRecommendationAsync();
        if (result is null) return NoContent();
        return Ok(result);
    }

    /// <summary>Trigger a new AI analysis (throttled to once per 30 minutes).</summary>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(AnalysisResultDto), 200)]
    [ProducesResponseType(429)]
    public async Task<ActionResult<AnalysisResultDto>> RefreshRecommendation()
    {
        if (DateTime.UtcNow - _lastRefreshAt < RefreshCooldown)
        {
            var retryAfter = (int)(RefreshCooldown - (DateTime.UtcNow - _lastRefreshAt)).TotalSeconds;
            Response.Headers.Append("Retry-After", retryAfter.ToString());
            return StatusCode(429, new { message = $"Analysis refresh is throttled. Try again in {retryAfter} seconds." });
        }

        _logger.LogInformation("AI analysis refresh triggered.");
        _lastRefreshAt = DateTime.UtcNow;

        var result = await _analysisService.RefreshRecommendationAsync();
        return Ok(result);
    }
}
