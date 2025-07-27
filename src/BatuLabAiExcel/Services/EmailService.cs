using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Email service implementation using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly AppConfiguration.EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<AppConfiguration.EmailSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result> SendLicenseKeyEmailAsync(string toEmail, string userName, string licenseKey, string planType, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = "🎉 Your Office AI License Key is Ready!";
            var body = BuildLicenseKeyEmailBody(userName, licenseKey, planType);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send license key email to {Email}", toEmail);
            return Result.Failure($"Failed to send license key email: {ex.Message}");
        }
    }

    public async Task<Result> SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = "Welcome to Office AI - Batu Lab! 🚀";
            var body = BuildWelcomeEmailBody(userName);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            return Result.Failure($"Failed to send welcome email: {ex.Message}");
        }
    }

    public async Task<Result> SendLicenseExpirationWarningAsync(string toEmail, string userName, int daysRemaining, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"⚠️ Your Office AI License Expires in {daysRemaining} Days";
            var body = BuildExpirationWarningEmailBody(userName, daysRemaining);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiration warning email to {Email}", toEmail);
            return Result.Failure($"Failed to send expiration warning email: {ex.Message}");
        }
    }

    private async Task<Result> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.SmtpHost))
            {
                _logger.LogWarning("Email service not configured - SMTP host is missing");
                return Result.Failure("Email service not configured");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Configure security options
            var secureSocketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return Result.Failure($"Failed to send email: {ex.Message}");
        }
    }

    private string BuildLicenseKeyEmailBody(string userName, string licenseKey, string planType)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Your License Key</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .license-box {{ background: white; border: 2px solid #667eea; border-radius: 8px; padding: 20px; margin: 20px 0; text-align: center; }}
        .license-key {{ font-family: 'Courier New', monospace; font-size: 18px; font-weight: bold; color: #667eea; letter-spacing: 2px; word-break: break-all; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 License Activated!</h1>
            <p>Office AI - Batu Lab.</p>
        </div>
        <div class='content'>
            <h2>Hello {userName}!</h2>
            <p>Thank you for purchasing the <strong>{planType}</strong> plan! Your payment has been processed successfully.</p>
            
            <div class='license-box'>
                <h3>Your License Key:</h3>
                <div class='license-key'>{licenseKey}</div>
                <p><small>Keep this key safe - you'll need it to activate your license</small></p>
            </div>

            <h3>How to Activate:</h3>
            <ol>
                <li>Open Office AI - Batu Lab application</li>
                <li>Go to Account → License Settings</li>
                <li>Enter your license key above</li>
                <li>Click 'Activate License'</li>
            </ol>

            <p>Your license is now active and ready to use! You can start automating your Excel tasks with AI assistance immediately.</p>

            <a href='mailto:support@batulab.com' class='button'>Need Help?</a>
        </div>
        <div class='footer'>
            <p>© 2025 Batu Lab. All rights reserved.</p>
            <p>If you didn't purchase this license, please contact support immediately.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildWelcomeEmailBody(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to Office AI</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .feature {{ background: white; padding: 15px; margin: 10px 0; border-left: 4px solid #667eea; border-radius: 0 5px 5px 0; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚀 Welcome to Office AI!</h1>
            <p>Office AI - Batu Lab.</p>
        </div>
        <div class='content'>
            <h2>Hello {userName}!</h2>
            <p>Welcome to Office AI - Batu Lab! Your account has been created successfully and you have a <strong>1-day trial</strong> to explore our features.</p>

            <h3>What you can do with Office AI:</h3>
            
            <div class='feature'>
                <h4>📊 Smart Excel Automation</h4>
                <p>Automate complex Excel tasks using natural language commands</p>
            </div>
            
            <div class='feature'>
                <h4>🤖 AI-Powered Analysis</h4>
                <p>Get insights and analysis from your spreadsheet data</p>
            </div>
            
            <div class='feature'>
                <h4>📈 Chart & Pivot Generation</h4>
                <p>Create professional charts and pivot tables instantly</p>
            </div>

            <p>Start by trying a simple command like: <em>""Create a budget spreadsheet with sample data""</em></p>

            <a href='mailto:support@batulab.com' class='button'>Get Started</a>
        </div>
        <div class='footer'>
            <p>© 2025 Batu Lab. All rights reserved.</p>
            <p>Questions? Reply to this email or contact support@batulab.com</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildExpirationWarningEmailBody(string userName, int daysRemaining)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>License Expiration Warning</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .warning-box {{ background: #fff3cd; border: 2px solid #ffc107; border-radius: 8px; padding: 20px; margin: 20px 0; text-align: center; }}
        .button {{ display: inline-block; background: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ License Expiring Soon</h1>
            <p>Office AI - Batu Lab.</p>
        </div>
        <div class='content'>
            <h2>Hello {userName}!</h2>
            
            <div class='warning-box'>
                <h3>Your license expires in {daysRemaining} days</h3>
                <p>Don't lose access to your AI-powered Excel automation!</p>
            </div>

            <p>To continue using Office AI without interruption, please renew your license before it expires.</p>

            <h3>Renewal Benefits:</h3>
            <ul>
                <li>Uninterrupted access to all AI features</li>
                <li>Continued Excel automation capabilities</li>
                <li>Priority customer support</li>
                <li>Latest feature updates</li>
            </ul>

            <a href='mailto:billing@batulab.com' class='button'>Renew License</a>
        </div>
        <div class='footer'>
            <p>© 2025 Batu Lab. All rights reserved.</p>
            <p>Need help? Contact support@batulab.com</p>
        </div>
    </div>
</body>
</html>";
    }
}