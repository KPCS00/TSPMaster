using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TSPMaster.API.Dtos.Allocations;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationService _allocationService;

    public AllocationsController(IAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found.");

    /// <summary>Get complete allocations overview (balance, allocations, status, move history, AI recommendation).</summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(AllocationOverviewDto), 200)]
    public async Task<ActionResult<AllocationOverviewDto>> GetOverview()
    {
        var overview = await _allocationService.GetOverviewAsync(UserId);
        return Ok(overview);
    }

    /// <summary>Get the current user's fund allocations.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AllocationDto>), 200)]
    public async Task<ActionResult<List<AllocationDto>>> GetAllocations()
    {
        var allocations = await _allocationService.GetAllocationsAsync(UserId);
        return Ok(allocations);
    }

    /// <summary>Get the current user's monthly transfer status (remaining IFT moves).</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TransferStatusDto), 200)]
    public async Task<ActionResult<TransferStatusDto>> GetTransferStatus()
    {
        var status = await _allocationService.GetTransferStatusAsync(UserId);
        return Ok(status);
    }

    /// <summary>Set the current user's initial TSP balance.</summary>
    [HttpPost("initial-balance")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetInitialBalance([FromBody] SetBalanceRequest request)
    {
        try
        {
            await _allocationService.SetInitialBalanceAsync(UserId, request.Balance, request.EffectiveDate);
            return Ok(new { message = "Initial balance saved successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Record a new Interfund Transfer (IFT) move executed on tsp.gov.</summary>
    [HttpPost("move")]
    [ProducesResponseType(typeof(AllocationMoveDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AllocationMoveDto>> RecordMove([FromBody] RecordMoveRequest request)
    {
        try
        {
            var move = await _allocationService.RecordMoveAsync(UserId, request);
            return Ok(move);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get the user's history of recorded IFT moves.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<AllocationMoveDto>), 200)]
    public async Task<ActionResult<List<AllocationMoveDto>>> GetMoveHistory()
    {
        var history = await _allocationService.GetMoveHistoryAsync(UserId);
        return Ok(history);
    }

    /// <summary>Delete/revert a recorded move by ID.</summary>
    [HttpDelete("move/{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeleteMove(int id)
    {
        try
        {
            await _allocationService.DeleteMoveAsync(UserId, id);
            return Ok(new { message = "Move deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Set current fund allocations (legacy endpoint, calls RecordMoveAsync).</summary>
    [HttpPut]
    [ProducesResponseType(typeof(List<AllocationDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<List<AllocationDto>>> SetAllocations([FromBody] AllocationRequest request)
    {
        if (request.Allocations is null || !request.Allocations.Any())
            return BadRequest(new { message = "At least one allocation is required." });

        if (request.Allocations.Any(a => a.Percentage < 0))
            return BadRequest(new { message = "Percentages cannot be negative." });

        try
        {
            await _allocationService.SetAllocationsAsync(UserId, request.Allocations);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var updated = await _allocationService.GetAllocationsAsync(UserId);
        return Ok(updated);
    }
}
