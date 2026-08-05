using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace TSPMaster.API.Helpers;

/// <summary>
/// Email service implemented via AWS Simple Email Service (SES).
/// Requires AWS credentials in environment variables or IAM role.
/// </summary>
public class SesEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SesEmailService> _logger;

    public SesEmailService(IConfiguration config, ILogger<SesEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var senderEmail = _config["AWS:SenderEmail"]
            ?? throw new InvalidOperationException("AWS:SenderEmail not configured.");

        try
        {
            var client = CreateSesClient();
            var request = new SendEmailRequest
            {
                Source = $"TSP Master <{senderEmail}>",
                Destination = new Destination { ToAddresses = [toEmail] },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content { Charset = "UTF-8", Data = htmlBody },
                        Text = new Content { Charset = "UTF-8", Data = HtmlToPlainText(htmlBody) }
                    }
                }
            };

            await client.SendEmailAsync(request);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
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
        string toEmail, string firstName, string recommendationSummary, string actionAdvice, int remainingTransfers, string currentMonth)
    {
        var subject = $"⏰ [10:30 AM CST Alert] TSP Daily Strategy Recommendation ({DateTime.UtcNow:MMM dd})";
        var html = $"""
            <html><body style="font-family: Arial, sans-serif; max-width: 650px; margin: 0 auto; color: #1e293b;">
              <div style="background: linear-gradient(135deg, #0f172a, #1e3a8a); padding: 25px; border-radius: 8px 8px 0 0; text-align: center;">
                <h1 style="color: #60a5fa; margin: 0; font-size: 24px;">📊 TSP Master Morning Briefing</h1>
                <div style="color: #fbbf24; font-size: 13px; font-weight: bold; margin-top: 6px;">
                  ⏰ 10:30 AM CST — 30 Minutes Before 11:00 AM CST TSP.gov Trade Deadline
                </div>
              </div>
              
              <div style="padding: 25px; background: #ffffff; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 8px 8px;">
                <p style="font-size: 16px; font-weight: bold; color: #0f172a; margin-top: 0;">Hi {firstName},</p>
                
                <p style="font-size: 14px; line-height: 1.6;">
                  Here is your daily AI-driven strategy briefing for <strong>{currentMonth}</strong>. To ensure trades execute overnight and take effect tomorrow, any Interfund Transfers (IFT) must be submitted on <strong>TSP.gov before 11:00 AM CST</strong> today.
                </p>

                <!-- Action Directive Box -->
                <div style="background: #eff6ff; border-left: 5px solid #2563eb; padding: 15px; border-radius: 6px; margin: 20px 0;">
                  <div style="font-size: 11px; font-weight: bold; color: #1d4ed8; text-transform: uppercase; letter-spacing: 0.5px;">
                    🎯 Recommended Daily Action
                  </div>
                  <div style="font-size: 16px; font-weight: bold; color: #1e293b; margin-top: 4px;">
                    {actionAdvice}
                  </div>
                  <div style="font-size: 12px; color: #64748b; margin-top: 6px;">
                    Remaining IFT Moves for {currentMonth}: <strong>{remainingTransfers} of 3</strong>
                  </div>
                </div>

                <!-- Briefing Details -->
                <div style="background: #f8fafc; border: 1px solid #cbd5e1; padding: 15px; border-radius: 6px; margin-bottom: 20px;">
                  <div style="font-size: 13px; font-weight: bold; color: #334155; margin-bottom: 8px;">
                    📈 Market & Monthly Strategy Summary
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

    private AmazonSimpleEmailServiceClient CreateSesClient()
    {
        var region = RegionEndpoint.GetBySystemName(_config["AWS:Region"] ?? "us-east-1");

        // Try environment variables / IAM role first; fall back to config
        var accessKey = _config["AWS:AccessKey"];
        var secretKey = _config["AWS:SecretKey"];

        if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            return new AmazonSimpleEmailServiceClient(
                new BasicAWSCredentials(accessKey, secretKey), region);

        return new AmazonSimpleEmailServiceClient(region);
    }

    private static string HtmlToPlainText(string html)
    {
        // Basic strip - for production consider a proper library
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
               .Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
               .Replace("  ", " ").Trim();
    }
}
