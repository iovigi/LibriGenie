namespace LibriGenie.Workers.Services.Brevo;

public interface IMailService
{
    System.Threading.Tasks.Task SendTextFromNoReply(string to, string subject, string body, CancellationToken cancellationToken);
}
