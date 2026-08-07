using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace TSPMaster.API.Helpers;

/// <summary>
/// Email service implemented via standard SMTP (System.Net.Mail).
/// Configured via "Smtp" section in appsettings.json.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _config["Smtp:Host"] ?? "localhost";
        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var enableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) ? ssl : true;
        var userName = _config["Smtp:UserName"];
        var password = _config["Smtp:Password"];
        var senderEmail = _config["Smtp:SenderEmail"] ?? "noreply@tspmaster.com";
        var senderName = _config["Smtp:SenderName"] ?? "TSP Master";

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        // Add alternate view for plain text fallback
        var plainText = HtmlToPlainText(htmlBody);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain"));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
        {
            client.Credentials = new NetworkCredential(userName, password);
        }

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("SMTP email sent to {Email}: {Subject} via {Host}:{Port}", toEmail, subject, host, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMTP email to {Email} via {Host}:{Port}", toEmail, host, port);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var html = $"""
            <html><body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <div style="background: linear-gradient(135deg, #1a237e, #0d47a1); padding: 30px; border-radius: 8px 8px 0 0;">
                <h1 style="color: white; margin: 0;">🏦 Welcome to TSP Master</h1>
              </div>
              <div style="padding: 30px; background: #f8f9fa; border-radius: 0 0 8px 8px;">
                <h2>Hi {firstName},</h2>
                <p>Welcome to <strong>TSP Master</strong> — your AI-powered Thrift Savings Plan analysis tool!</p>
                <p>With TSP Master you can:</p>
                <ul>
                  <li>📈 Track real-time TSP fund prices from TSP.gov</li>
                  <li>🎯 Set and monitor your contribution allocations</li>
                  <li>🤖 Get AI-powered investment recommendations</li>
                  <li>📊 View your portfolio performance vs fund benchmarks</li>
                </ul>
                <p>Get started by setting your fund allocations in the dashboard.</p>
                <p style="color: #666; font-size: 12px;">
                  <em>This application is for informational purposes only and does not constitute financial advice.</em>
                </p>
              </div>
            </body></html>
            """;

        await SendEmailAsync(toEmail, "Welcome to TSP Master!", html);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
    {
        var html = $"""
            <html><body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <div style="background: linear-gradient(135deg, #1a237e, #0d47a1); padding: 30px; border-radius: 8px 8px 0 0;">
                <h1 style="color: white; margin: 0;">🔐 Password Reset</h1>
              </div>
              <div style="padding: 30px; background: #f8f9fa; border-radius: 0 0 8px 8px;">
                <h2>Hi {firstName},</h2>
                <p>We received a request to reset your TSP Master password.</p>
                <p>Click the button below to reset your password (expires in 1 hour):</p>
                <div style="text-align: center; margin: 30px 0;">
                  <a href="{resetLink}" style="background: #1a237e; color: white; padding: 12px 30px; 
                     border-radius: 6px; text-decoration: none; font-weight: bold;">
                    Reset Password
                  </a>
                </div>
                <p style="color: #666; font-size: 12px;">If you did not request a password reset, you can safely ignore this email.</p>
              </div>
            </body></html>
            """;

        await SendEmailAsync(toEmail, "Reset Your TSP Master Password", html);
    }

    public async Task SendDailyRecommendationEmailAsync(
        string toEmail,
        string firstName,
        string recommendationSummary,
        string actionAdvice,
        int remainingTransfers,
        string currentMonth,
        string tomorrowEffectiveDate = "",
        string intradaySummary = "",
        string seasonalitySummary = "")
    {
        var targetTomorrowText = string.IsNullOrWhiteSpace(tomorrowEffectiveDate) ? "Tomorrow" : tomorrowEffectiveDate;
        var subject = $"⏰ [10:30 AM CST Alert] TSP Strategy Directive for Tomorrow ({targetTomorrowText})";

        var intradayHtml = string.IsNullOrWhiteSpace(intradaySummary) ? "" : $"""
            <div style="background: #f0fdf4; border-left: 4px solid #16a34a; padding: 12px 15px; border-radius: 4px; margin-bottom: 15px;">
              <div style="font-size: 11px; font-weight: bold; color: #15803d; text-transform: uppercase;">
                ⚡ Live Morning Snapshot (Open to 10:30 AM CST)
              </div>
              <div style="font-size: 13px; color: #166534; margin-top: 4px;">
                {intradaySummary}
              </div>
            </div>
            """;

        var seasonalityHtml = string.IsNullOrWhiteSpace(seasonalitySummary) ? "" : $"""
            <div style="background: #f8fafc; border: 1px solid #e2e8f0; padding: 12px 15px; border-radius: 6px; margin-bottom: 15px;">
              <div style="font-size: 12px; font-weight: bold; color: #334155; margin-bottom: 6px;">
                📅 Multi-Decade Historical Seasonality & Trading Day Insights
              </div>
              <div style="font-size: 12px; color: #475569; line-height: 1.5; white-space: pre-line;">
                {seasonalitySummary}
              </div>
            </div>
            """;

        var html = $"""
            <html><body style="font-family: Arial, sans-serif; max-width: 650px; margin: 0 auto; color: #1e293b;">
              <div style="background: linear-gradient(135deg, #0f172a, #1e3a8a); padding: 25px; border-radius: 8px 8px 0 0; text-align: center;">
                <h1 style="color: #60a5fa; margin: 0; font-size: 24px;">📊 TSP Master Morning Briefing</h1>
                <div style="color: #fbbf24; font-size: 13px; font-weight: bold; margin-top: 6px;">
                  ⏰ 10:30 AM CST — Position your portfolio for Effective Date: <strong>{targetTomorrowText}</strong>
                </div>
              </div>
              
              <div style="padding: 25px; background: #ffffff; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 8px 8px;">
                <p style="font-size: 16px; font-weight: bold; color: #0f172a; margin-top: 0;">Hi {firstName},</p>
                
                <p style="font-size: 14px; line-height: 1.6;">
                  Here is your daily AI & quantitative strategy briefing for <strong>{currentMonth}</strong>. To ensure trades execute overnight and become effective on <strong>{targetTomorrowText}</strong>, submit your Interfund Transfer (IFT) on <strong>TSP.gov before 11:00 AM CST</strong> today.
                </p>

                <!-- Action Directive Box -->
                <div style="background: #eff6ff; border-left: 5px solid #2563eb; padding: 15px; border-radius: 6px; margin: 20px 0;">
                  <div style="font-size: 11px; font-weight: bold; color: #1d4ed8; text-transform: uppercase; letter-spacing: 0.5px;">
                    🎯 Recommended Directive for Tomorrow ({targetTomorrowText})
                  </div>
                  <div style="font-size: 16px; font-weight: bold; color: #1e293b; margin-top: 4px;">
                    {actionAdvice}
                  </div>
                  <div style="font-size: 12px; color: #64748b; margin-top: 6px;">
                    Remaining IFT Moves for {currentMonth}: <strong>{remainingTransfers} of 3</strong>
                  </div>
                </div>

                {intradayHtml}

                {seasonalityHtml}

                <!-- Briefing Details -->
                <div style="background: #ffffff; border: 1px solid #cbd5e1; padding: 15px; border-radius: 6px; margin-bottom: 20px;">
                  <div style="font-size: 13px; font-weight: bold; color: #334155; margin-bottom: 8px;">
                    🤖 AI Analysis & Execution Reasoning
                  </div>
                  <div style="font-size: 13px; color: #475569; line-height: 1.5; white-space: pre-line;">
                    {recommendationSummary}
                  </div>
                </div>

                <!-- TSP.gov Link CTA -->
                <div style="text-align: center; margin: 25px 0 15px;">
                  <a href="https://www.tsp.gov" target="_blank" style="background: #2563eb; color: #ffffff; padding: 12px 28px; border-radius: 6px; text-decoration: none; font-weight: bold; font-size: 14px; display: inline-block;">
                    Log into TSP.gov to Submit Trade &rarr;
                  </a>
                </div>

                <p style="color: #94a3b8; font-size: 11px; text-align: center; margin-top: 25px; line-height: 1.4;">
                  <em>Notice: Interfund transfers must be submitted on TSP.gov prior to 11:00 AM CST (12:00 PM EST). This automated email is provided for informational and planning purposes only and does not constitute financial advice.</em>
                </p>
              </div>
            </body></html>
            """;

        await SendEmailAsync(toEmail, subject, html);
    }

    private static string HtmlToPlainText(string html)
    {
        return Regex.Replace(html, "<[^>]+>", " ")
               .Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
               .Replace("  ", " ").Trim();
    }
}
