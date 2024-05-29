using brevo_csharp.Api;
using brevo_csharp.Model;
using LibriGenie.Workers.Configuration;

namespace LibriGenie.Workers.Services.Brevo;

public class MailService(AppSettings appSettings) : IMailService
{
    private const string API_KEY = "api-key";
    private const string NO_REPLY = "No-Reply";

    private readonly AppSettings appSettings = appSettings;

    public async System.Threading.Tasks.Task SendTextFromNoReply(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var apiInstance = new TransactionalEmailsApi(new brevo_csharp.Client.Configuration() { ApiKey = new Dictionary<string, string>() { { API_KEY, appSettings.MailSettings.BrevoApiKey } } });

        CreateSmtpEmail _ = await apiInstance.SendTransacEmailAsync(CreateSendSmtpEmail(to, subject, body, null));
    }

    private SendSmtpEmail CreateSendSmtpEmail(string to, string subject, string? body, string? html)
    {
        brevo_csharp.Client.Configuration.Default.ApiKey.Add(API_KEY, appSettings.MailSettings.BrevoApiKey);
        string ApiKey = API_KEY;

        string SenderName = NO_REPLY;
        string SenderEmail = appSettings.MailSettings.NoReplyEmail;
        SendSmtpEmailSender Email = new(SenderName, SenderEmail);
        string ToEmail = to;
        string ToName = to;
        SendSmtpEmailTo smtpEmailTo = new(ToEmail, ToName);
        List<SendSmtpEmailTo> To = [smtpEmailTo];

        return new SendSmtpEmail(Email, To, null, null, html, body, subject, null, null, null, null, null, null, null);
    }
}
