using LibriGenie.Workers.Configuration;
using System.Net.Mail;

namespace LibriGenie.Workers.Services;

public class MailService(AppSettings settings, ILogger<MailService> logger) : IMailService
{
    public async Task Send(string to, string subject, string body, CancellationToken cancellationToken)
    {
        SmtpClient smtp = new()
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new System.Net.NetworkCredential(settings.GmailSettings.Email, settings.GmailSettings.Password),
            Timeout = 30000,
        };


        MailMessage message = new(settings.GmailSettings.Email, to, subject, body) { IsBodyHtml = false };

        await smtp.SendMailAsync(message, cancellationToken);
    }
}
