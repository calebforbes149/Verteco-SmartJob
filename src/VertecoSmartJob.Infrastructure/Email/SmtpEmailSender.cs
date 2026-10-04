using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VertecoSmartJob.Application.Abstractions;

namespace VertecoSmartJob.Infrastructure.Email;

public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = configuration["Smtp:Host"];
        var port = configuration.GetValue<int?>("Smtp:Port");
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        var fromAddress = configuration["Smtp:FromAddress"] ?? username;

        if (string.IsNullOrWhiteSpace(host) || port is null || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            // Fail safe, not silently — a misconfigured mail server should never
            // block a password reset from completing if the rest of the flow succeeded.
            logger.LogWarning("SMTP is not configured; email to {Email} with subject '{Subject}' was not sent.", toEmail, subject);
            return;
        }

        using var message = new MailMessage(fromAddress!, toEmail, subject, htmlBody) { IsBodyHtml = true };
        using var client = new SmtpClient(host, port.Value)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Same reasoning: log it, don't let a flaky mail server break the caller's flow.
            logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'.", toEmail, subject);
        }
    }
}