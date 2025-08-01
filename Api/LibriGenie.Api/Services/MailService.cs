
using LibriGenie.Api.Configuration;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace LibriGenie.Api.Services;

public class MailService(AppSettings settings, ILogger<MailService> logger) : IMailService
{
    public async Task Send(string to, string subject, string body, CancellationToken cancellationToken)
    {
        await SendViaGMail(to, subject, body, cancellationToken);
    }

    private async Task SendViaHMail(string to, string subject, string body, CancellationToken cancellationToken)
    {
        SmtpClient smtp = new()
        {
            Host = settings.hMail.Host,
            Port = settings.hMail.Port,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new System.Net.NetworkCredential(settings.hMail.NoReplyEmail, settings.hMail.NoReplyPassword),
            Timeout = 30000,
        };

        smtp.EnableSsl = false; // unless you enabled SSL

        MailMessage message = new(settings.hMail.NoReplyEmail, to, subject, body) { IsBodyHtml = false };

        await smtp.SendMailAsync(message, cancellationToken);
    }

    private async Task SendViaGMail(string to, string subject, string body, CancellationToken cancellationToken)
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
