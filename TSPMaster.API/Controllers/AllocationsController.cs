using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TSPMaster.API.Dtos.Allocations;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("[controller]")]
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

    /// <summary>Set the current user's fund allocations. Percentages must sum to 100.</summary>
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
