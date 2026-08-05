using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TSPMaster.API.Dtos.Users;
using TSPMaster.API.Models;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPortfolioService _portfolioService;

    public UsersController(UserManager<ApplicationUser> userManager, IPortfolioService portfolioService)
    {
        _userManager = userManager;
        _portfolioService = portfolioService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found.");

    /// <summary>Get the current user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        return Ok(new UserProfileDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.CreatedAt,
            user.LastLoginAt));
    }

    /// <summary>Get the current user's portfolio performance over time.</summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PerformanceSummaryDto), 200)]
    public async Task<ActionResult<PerformanceSummaryDto>> GetPerformance([FromQuery] int days = 90)
    {
        if (days < 7 || days > 1825)
            return BadRequest(new { message = "Days must be between 7 and 1825 (5 years)." });

        var summary = await _portfolioService.GetPerformanceSummaryAsync(UserId, days);
        return Ok(summary);
    }
}
