using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TSPMaster.API.Dtos.Auth;
using TSPMaster.API.Helpers;
using TSPMaster.API.Models;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ITspDataService _tspData;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEmailService emailService,
        ITspDataService tspData,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _tspData = tspData;
        _config = config;
        _logger = logger;
    }

    /// <summary>Register a new user with email and password.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Registration failed for {Email}: {Errors}",
                request.Email, string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        await _userManager.AddToRoleAsync(user, "User");

        // Send welcome email (non-blocking)
        _ = Task.Run(async () =>
        {
            try { await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email); }
        });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        _logger.LogInformation("User registered: {Email}", user.Email);
        await _tspData.EnsurePricesUpToDateAsync();
        return Ok(new AuthResponse(token, DateTime.UtcNow.AddHours(24), user.Id, user.Email!, user.FirstName, user.LastName));
    }

    /// <summary>Login with email and password, returns JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut) return Unauthorized(new { message = "Account is locked out." });
            return Unauthorized(new { message = "Invalid email or password." });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        _logger.LogInformation("User logged in: {Email}", user.Email);
        await _tspData.EnsurePricesUpToDateAsync();
        return Ok(new AuthResponse(token, DateTime.UtcNow.AddHours(24), user.Id, user.Email!, user.FirstName, user.LastName));
    }
}
