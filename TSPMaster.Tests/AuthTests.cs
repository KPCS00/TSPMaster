using TSPMaster.API.Dtos.Auth;
using Xunit;

namespace TSPMaster.Tests;

public class AuthTests
{
    [Fact]
    public void ForgotPasswordRequest_ShouldStoreEmailCorrectly()
    {
        // Arrange & Act
        var request = new ForgotPasswordRequest("user@agency.gov");

        // Assert
        Assert.Equal("user@agency.gov", request.Email);
    }

    [Fact]
    public void ResetPasswordRequest_ShouldStorePropertiesCorrectly()
    {
        // Arrange & Act
        var request = new ResetPasswordRequest("user@agency.gov", "sample-token-123", "NewPassword123!");

        // Assert
        Assert.Equal("user@agency.gov", request.Email);
        Assert.Equal("sample-token-123", request.Token);
        Assert.Equal("NewPassword123!", request.NewPassword);
    }
}
