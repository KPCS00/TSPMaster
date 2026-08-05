using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSPMaster.API.Dtos.Funds;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("[controller]")]
public class FundsController : ControllerBase
{
    private readonly ITspDataService _tspData;
    private readonly ILogger<FundsController> _logger;

    public FundsController(ITspDataService tspData, ILogger<FundsController> logger)
    {
        _tspData = tspData;
        _logger = logger;
    }

    /// <summary>Get the latest closing price for all TSP funds with day-over-day change.</summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(List<FundLatestDto>), 200)]
    public async Task<ActionResult<List<FundLatestDto>>> GetLatest()
    {
        var prices = await _tspData.GetLatestPricesAsync();
        return Ok(prices);
    }

    /// <summary>Get all available fund names.</summary>
    [HttpGet("names")]
    public ActionResult<IReadOnlyList<string>> GetFundNames()
    {
        return Ok(_tspData.GetAllFundNames());
    }

    /// <summary>Get price history for a specific fund over a date range.</summary>
    [HttpGet("{fundName}/history")]
    [ProducesResponseType(typeof(List<FundPriceDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<List<FundPriceDto>>> GetFundHistory(
        [FromRoute] string fundName,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        var effectiveTo = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveFrom = from ?? effectiveTo.AddDays(-365);

        if (effectiveFrom > effectiveTo)
            return BadRequest(new { message = "From date must be before To date." });

        var history = await _tspData.GetFundHistoryAsync(fundName, effectiveFrom, effectiveTo);
        return Ok(history);
    }

    /// <summary>Get price history for all funds over a date range (for charting).</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(Dictionary<string, List<FundPriceDto>>), 200)]
    public async Task<ActionResult<Dictionary<string, List<FundPriceDto>>>> GetAllFundsHistory(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        var effectiveTo = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveFrom = from ?? effectiveTo.AddDays(-180);

        var history = await _tspData.GetAllFundsHistoryAsync(effectiveFrom, effectiveTo);
        return Ok(history);
    }

    /// <summary>Manually trigger a TSP data sync (Admin only).</summary>
    [HttpPost("sync")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TriggerSync([FromServices] ITspDataService tspData)
    {
        _logger.LogInformation("Manual TSP sync triggered by admin.");
        await tspData.SyncAsync();
        return Ok(new { message = "TSP data sync completed." });
    }
}
