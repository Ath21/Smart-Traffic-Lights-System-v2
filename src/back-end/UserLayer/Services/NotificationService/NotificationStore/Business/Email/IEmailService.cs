namespace NotificationStore.Business.Email;

public interface IEmailService
{
    Task SendEmailAsync(string recipientEmail, string subject, string body);
}
