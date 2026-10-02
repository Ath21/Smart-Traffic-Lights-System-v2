using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationStore.Models;
using NotificationStore.Publishers.Logs;

namespace NotificationStore.Business.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly INotificationLogPublisher _logPublisher;

    private const string Domain = "[BUSINESS][EMAIL]";

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        INotificationLogPublisher logPublisher)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _logPublisher = logPublisher;
    }

    public async Task SendEmailAsync(string recipientEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("{Domain} Missing recipient email, skipping send.", Domain);
            await _logPublisher.PublishAuditAsync(
                domain: Domain,
                messageText: $"{Domain} Skipped email send (no recipient provided).",
                category: "EMAIL",
                operation: "SendEmailAsync");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("{Domain} Email sent successfully to {Recipient} | Subject: {Subject}", Domain, recipientEmail, subject);

            await _logPublisher.PublishAuditAsync(
                domain: Domain,
                messageText: $"{Domain} Email sent successfully.",
                category: "EMAIL",
                operation: "SendEmailAsync",
                data: new Dictionary<string, object>
                {
                    ["Recipient"] = recipientEmail,
                    ["Subject"] = subject
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Domain} Failed to send email to {Recipient} | Subject: {Subject}", Domain, recipientEmail, subject);

            await _logPublisher.PublishErrorAsync(
                domain: Domain,
                messageText: $"{Domain} Failed to send email: {ex.Message}",
                operation: "SendEmailAsync",
                data: new Dictionary<string, object>
                {
                    ["Recipient"] = recipientEmail,
                    ["Subject"] = subject,
                    ["Exception"] = ex.Message,
                    ["StackTrace"] = ex.StackTrace ?? string.Empty
                });

            throw; // rethrow to middleware
        }
    }
}
