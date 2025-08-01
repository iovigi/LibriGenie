namespace LibriGenie.Workers.Services;

public interface ILibriGenieClient
{
    Task<List<Models.Task>> GetTasksForRun(int page, int pageSize, CancellationToken cancellationToken);
    Task<List<Models.Task>> GetAllActiveTasks(CancellationToken cancellationToken);
    Task SetLastRun(string id, CancellationToken cancellationToken);
    Task SendTextFromNoReply(string to, string subject, string body, CancellationToken cancellationToken);
}
