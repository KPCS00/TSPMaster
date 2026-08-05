using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSPMaster.API.Dtos.Analysis;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("[controller]")]
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

    /// <summary>Manually trigger the 10:30 AM CST daily recommendation email dispatch (Admin/Test).</summary>
    [HttpPost("daily-email/test")]
    [Authorize]
    public async Task<IActionResult> TriggerDailyEmail([FromServices] IEnumerable<IHostedService> services)
    {
        var dailyService = services.OfType<TspDailyRecommendationService>().FirstOrDefault();
        if (dailyService is not null)
        {
            _logger.LogInformation("Manual trigger for 10:30 AM CST Daily Email Briefing.");
            await dailyService.ProcessDailyRecommendationsAsync();
            return Ok(new { message = "Daily 10:30 AM CST strategy emails processed and dispatched." });
        }
        return BadRequest(new { message = "TspDailyRecommendationService is not active." });
    }
}
