namespace TSPMaster.API.Helpers;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendWelcomeEmailAsync(string toEmail, string firstName);
    Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink);
    Task SendDailyRecommendationEmailAsync(
        string toEmail,
        string firstName,
        string recommendationSummary,
        string actionAdvice,
        int remainingTransfers,
        string currentMonth,
        string tomorrowEffectiveDate = "",
        string intradaySummary = "",
        string seasonalitySummary = "");
}
