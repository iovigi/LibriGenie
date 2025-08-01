namespace LibriGenie.Api.Services;

public interface IMailService
{
    Task Send(string to, string subject, string body, CancellationToken cancellationToken);
}
