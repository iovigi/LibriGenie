namespace LibriGenie.Workers.Services;

public interface IWordpressPublisher
{
    Task Publish(Models.Task task, string title, string content, CancellationToken cancellationToken);
}
