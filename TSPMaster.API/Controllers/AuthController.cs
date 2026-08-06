using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TSPMaster.API.Dtos.Auth;
using TSPMaster.API.Helpers;
using TSPMaster.API.Models;
using TSPMaster.API.Services;

namespace TSPMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    private int ExpirationHours =>
        int.TryParse(_config["JwtSettings:ExpirationHours"], out var hours) ? hours : 24;

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

        // Send welcome email safely in background scope
        var email = user.Email!;
        var firstName = user.FirstName;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendWelcomeEmailAsync(email, firstName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", email);
            }
        });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        _logger.LogInformation("User registered: {Email}", user.Email);
        return Ok(new AuthResponse(token, DateTime.UtcNow.AddHours(ExpirationHours), user.Id, user.Email!, user.FirstName, user.LastName));
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
        return Ok(new AuthResponse(token, DateTime.UtcNow.AddHours(ExpirationHours), user.Id, user.Email!, user.FirstName, user.LastName));
    }

    /// <summary>Request password reset link by email.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var clientUrl = _config["ClientUrl"] ?? "http://localhost:5173";
            var resetLink = $"{clientUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

            var email = user.Email!;
            var firstName = user.FirstName;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailService.SendPasswordResetEmailAsync(email, firstName, resetLink);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send password reset email to {Email}", email);
                }
            });
        }

        // Return 200 OK regardless to avoid revealing email existence
        return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    /// <summary>Reset password using token.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Email, token, and new password are required." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { message = "Invalid email or token." });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        _logger.LogInformation("Password reset successfully for {Email}", user.Email);
        return Ok(new { message = "Password reset successfully. You can now log in with your new password." });
    }
}

