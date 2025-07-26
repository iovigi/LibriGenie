namespace LibriGenie.Api.Services;

public interface ITaskService
{
    Task SetLastRun(string id, CancellationToken cancellationToken);

    Task<IList<Models.Task>> GetTasksForRun(int page, int pageSize,CancellationToken cancellationToken);
    
    Task<IList<Models.Task>> GetAllActiveTasks(CancellationToken cancellationToken);
}
