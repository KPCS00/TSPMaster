using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TSPMaster.API.Helpers;
using Xunit;

namespace TSPMaster.Tests;

public class SmtpEmailServiceTests
{
    [Fact]
    public void SmtpEmailService_ShouldInitializeWithConfiguration()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Smtp:Host", "smtp.test.com"},
            {"Smtp:Port", "587"},
            {"Smtp:EnableSsl", "true"},
            {"Smtp:UserName", "testuser"},
            {"Smtp:Password", "secretpassword"},
            {"Smtp:SenderEmail", "test@tspmaster.com"},
            {"Smtp:SenderName", "TSP Master Test"}
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = NullLogger<SmtpEmailService>.Instance;

        // Act
        var service = new SmtpEmailService(config, logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldThrowWhenHostIsInvalid_WithoutCrashingConstructor()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Smtp:Host", "127.0.0.1"},
            {"Smtp:Port", "59999"}, // Closed port
            {"Smtp:EnableSsl", "false"},
            {"Smtp:SenderEmail", "test@tspmaster.com"}
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = NullLogger<SmtpEmailService>.Instance;
        var service = new SmtpEmailService(config, logger);

        // Act & Assert (Attempting to send to offline port should throw SmtpException without crashing)
        await Assert.ThrowsAsync<System.Net.Mail.SmtpException>(() =>
            service.SendWelcomeEmailAsync("recipient@example.com", "John"));
    }
}
