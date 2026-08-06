using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using TSPMaster.API.Models;
using TSPMaster.API.Services;
using Xunit;

namespace TSPMaster.Tests;

public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_ShouldCreateValidJwtWithClaimsAndRoles()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:SecretKey", "TEST_SUPER_SECURE_SECRET_KEY_MINIMUM_32_CHARS!"},
            {"JwtSettings:Issuer", "TSPMasterTest"},
            {"JwtSettings:Audience", "TSPMasterUsersTest"},
            {"JwtSettings:ExpirationHours", "24"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new TokenService(config);
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var roles = new List<string> { "User", "Admin" };

        // Act
        var tokenString = service.GenerateToken(user, roles);

        // Assert
        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("TSPMasterTest", token.Issuer);
        Assert.Contains("TSPMasterUsersTest", token.Audiences);
        Assert.Equal("user-123", token.Subject);
        Assert.Contains(token.Claims, c => (c.Type == "email" || c.Type == System.Security.Claims.ClaimTypes.Email) && c.Value == "john.doe@example.com");
        Assert.Contains(token.Claims, c => (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) && c.Value == "User");
        Assert.Contains(token.Claims, c => (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) && c.Value == "Admin");
    }
}
