namespace LibriGenie.Workers.Services;

public interface ILibriGenieClient
{
    Task<List<Models.Task>> GetTasksForRun(int page, int pageSize, CancellationToken cancellationToken);
    Task SetLastRun(string id, CancellationToken cancellationToken);
}
