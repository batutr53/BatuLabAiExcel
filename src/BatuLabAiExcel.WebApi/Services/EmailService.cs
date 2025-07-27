using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using BatuLabAiExcel.WebApi.Models;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Email service implementation for Web API
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly AppConfiguration.EmailSettings _settings;

    public EmailService(ILogger<EmailService> logger, IOptions<AppConfiguration.EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<Result> SendLicenseKeyEmailAsync(string toEmail, string userName, string licenseKey, string planType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending license key email to: {Email}", toEmail);

            var subject = "🎉 Your Office AI - Batu Lab License Key";
            var htmlBody = CreateLicenseKeyEmailHtml(userName, licenseKey, planType);

            var result = await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("License key email sent successfully to: {Email}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending license key email to: {Email}", toEmail);
            return Result.Failure("Failed to send license key email");
        }
    }

    public async Task<Result> SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending welcome email to: {Email}", toEmail);

            var subject = "🚀 Welcome to Office AI - Batu Lab!";
            var htmlBody = CreateWelcomeEmailHtml(userName);

            var result = await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Welcome email sent successfully to: {Email}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to: {Email}", toEmail);
            return Result.Failure("Failed to send welcome email");
        }
    }

    public async Task<Result> SendLicenseExpiryWarningAsync(string toEmail, string userName, int daysRemaining, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending license expiry warning email to: {Email}", toEmail);

            var subject = $"⚠️ Your Office AI License Expires in {daysRemaining} Days";
            var htmlBody = CreateExpiryWarningEmailHtml(userName, daysRemaining);

            var result = await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("License expiry warning email sent successfully to: {Email}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending license expiry warning email to: {Email}", toEmail);
            return Result.Failure("Failed to send license expiry warning email");
        }
    }

    public async Task<Result> SendPasswordResetEmailAsync(string toEmail, string userName, string tempPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending password reset email to: {Email}", toEmail);

            var subject = "🔐 Your Office AI Password Reset";
            var htmlBody = CreatePasswordResetEmailHtml(userName, tempPassword);

            var result = await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Password reset email sent successfully to: {Email}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to: {Email}", toEmail);
            return Result.Failure("Failed to send password reset email");
        }
    }

    private async Task<Result> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, 
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to: {Email}", toEmail);
            return Result.Failure("Failed to send email");
        }
    }

    private static string CreateLicenseKeyEmailHtml(string userName, string licenseKey, string planType)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Your License Key</title>
</head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 10px; text-align: center;'>
        <h1 style='margin: 0; font-size: 28px;'>🎉 License Key Delivered!</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Your Office AI - Batu Lab {planType} plan is ready</p>
    </div>
    
    <div style='padding: 30px; background: #f8f9fa; border-radius: 10px; margin: 20px 0;'>
        <h2 style='color: #333; margin-top: 0;'>Hello {userName}!</h2>
        <p style='color: #666; line-height: 1.6;'>Thank you for choosing Office AI - Batu Lab! Your {planType} plan has been activated.</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0;'>
            <h3 style='color: #333; margin-top: 0;'>Your License Key:</h3>
            <code style='background: #f1f3f4; padding: 10px; border-radius: 4px; font-size: 16px; display: block; word-break: break-all; color: #e91e63; font-weight: bold;'>{licenseKey}</code>
        </div>
        
        <h3 style='color: #333;'>🚀 How to Activate:</h3>
        <ol style='color: #666; line-height: 1.8;'>
            <li>Open Office AI - Batu Lab application</li>
            <li>Go to Settings → License</li>
            <li>Enter your license key above</li>
            <li>Click 'Activate License'</li>
            <li>Enjoy your premium features!</li>
        </ol>
        
        <div style='background: #e3f2fd; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 0; color: #1976d2;'><strong>💡 Tip:</strong> Save this email for your records. You'll need the license key if you reinstall the application.</p>
        </div>
    </div>
    
    <div style='text-align: center; color: #666; font-size: 14px; margin-top: 30px;'>
        <p>Questions? Contact us at support@batulab.com</p>
        <p style='margin: 5px 0;'>© 2025 Batu Lab. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private static string CreateWelcomeEmailHtml(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to Office AI</title>
</head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 10px; text-align: center;'>
        <h1 style='margin: 0; font-size: 28px;'>🚀 Welcome to Office AI!</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Your AI-powered Excel journey starts now</p>
    </div>
    
    <div style='padding: 30px; background: #f8f9fa; border-radius: 10px; margin: 20px 0;'>
        <h2 style='color: #333; margin-top: 0;'>Hello {userName}!</h2>
        <p style='color: #666; line-height: 1.6;'>Welcome to Office AI - Batu Lab! You've just joined thousands of users who are revolutionizing their Excel experience with AI.</p>
        
        <h3 style='color: #333;'>🎁 Your 1-Day Trial Includes:</h3>
        <ul style='color: #666; line-height: 1.8;'>
            <li>✅ Full access to Claude, Gemini & Groq AI</li>
            <li>✅ Unlimited Excel file processing</li>
            <li>✅ Advanced AI formulas and automation</li>
            <li>✅ Smart data analysis and insights</li>
        </ul>
        
        <div style='background: #e8f5e8; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 0; color: #2e7d32;'><strong>🎯 Pro Tip:</strong> Try asking the AI to analyze your data, create charts, or automate repetitive tasks!</p>
        </div>
    </div>
    
    <div style='text-align: center; color: #666; font-size: 14px; margin-top: 30px;'>
        <p>Questions? Contact us at support@batulab.com</p>
        <p style='margin: 5px 0;'>© 2025 Batu Lab. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private static string CreateExpiryWarningEmailHtml(string userName, int daysRemaining)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>License Expiry Warning</title>
</head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff9800 0%, #f57c00 100%); color: white; padding: 30px; border-radius: 10px; text-align: center;'>
        <h1 style='margin: 0; font-size: 28px;'>⚠️ License Expiring Soon</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Don't lose access to your AI-powered Excel features</p>
    </div>
    
    <div style='padding: 30px; background: #f8f9fa; border-radius: 10px; margin: 20px 0;'>
        <h2 style='color: #333; margin-top: 0;'>Hello {userName}!</h2>
        <p style='color: #666; line-height: 1.6;'>Your Office AI - Batu Lab license will expire in <strong>{daysRemaining} days</strong>. Don't let your productivity stop!</p>
        
        <div style='background: #fff3cd; padding: 15px; border-radius: 8px; border-left: 4px solid #ff9800; margin: 20px 0;'>
            <p style='margin: 0; color: #856404;'><strong>Action Required:</strong> Renew your license to continue enjoying AI-powered Excel automation.</p>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='#' style='background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Renew License Now</a>
        </div>
    </div>
    
    <div style='text-align: center; color: #666; font-size: 14px; margin-top: 30px;'>
        <p>Questions? Contact us at support@batulab.com</p>
        <p style='margin: 5px 0;'>© 2025 Batu Lab. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private static string CreatePasswordResetEmailHtml(string userName, string tempPassword)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset</title>
</head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 10px; text-align: center;'>
        <h1 style='margin: 0; font-size: 28px;'>🔐 Password Reset</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Your temporary password is ready</p>
    </div>
    
    <div style='padding: 30px; background: #f8f9fa; border-radius: 10px; margin: 20px 0;'>
        <h2 style='color: #333; margin-top: 0;'>Hello {userName}!</h2>
        <p style='color: #666; line-height: 1.6;'>We've generated a temporary password for your Office AI - Batu Lab account.</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0;'>
            <h3 style='color: #333; margin-top: 0;'>Temporary Password:</h3>
            <code style='background: #f1f3f4; padding: 10px; border-radius: 4px; font-size: 16px; display: block; color: #e91e63; font-weight: bold;'>{tempPassword}</code>
        </div>
        
        <div style='background: #ffebee; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 0; color: #c62828;'><strong>⚠️ Important:</strong> Please change this password immediately after logging in for security.</p>
        </div>
    </div>
    
    <div style='text-align: center; color: #666; font-size: 14px; margin-top: 30px;'>
        <p>Questions? Contact us at support@batulab.com</p>
        <p style='margin: 5px 0;'>© 2025 Batu Lab. All rights reserved.</p>
    </div>
</body>
</html>";
    }
}