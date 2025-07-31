using LibriGenie.Workers.Configuration;
using System.Net.Http.Json;

namespace LibriGenie.Workers.Services;

public class LibriGenieClient(HttpClient httpClient, AppSettings appSettings, ILogger<LibriGenieClient> logger) : ILibriGenieClient
{
    public async Task<List<Models.Task>> GetTasksForRun(int page, int pageSize,CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching tasks for run - Page: {page}, PageSize: {pageSize}", page, pageSize);
        
        httpClient.DefaultRequestHeaders.Authorization = appSettings.ApiConfiguration.GetAuthenticationHeaderValue();

        try
        {
            var tasks = await httpClient.GetFromJsonAsync<List<Models.Task>>($"{appSettings.ApiConfiguration.Endpoint}Task/GetTasksForRun?page={page}&pageSize={pageSize}", cancellationToken);
            logger.LogInformation("Successfully retrieved {count} tasks for run", tasks?.Count ?? 0);
            return tasks ?? new List<Models.Task>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get tasks for run: {message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Models.Task>> GetAllActiveTasks(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all active tasks");
        
        httpClient.DefaultRequestHeaders.Authorization = appSettings.ApiConfiguration.GetAuthenticationHeaderValue();

        try
        {
            var tasks = await httpClient.GetFromJsonAsync<List<Models.Task>>($"{appSettings.ApiConfiguration.Endpoint}Task/GetAllActiveTasks", cancellationToken);
            logger.LogInformation("Successfully retrieved {count} active tasks", tasks?.Count ?? 0);
            return tasks ?? new List<Models.Task>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all active tasks: {message}", ex.Message);
            throw;
        }
    }

    public async System.Threading.Tasks.Task SetLastRun(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting last run for task: {taskId}", id);
        
        httpClient.DefaultRequestHeaders.Authorization = appSettings.ApiConfiguration.GetAuthenticationHeaderValue();

        try
        {
            await httpClient.PostAsync($"{appSettings.ApiConfiguration.Endpoint}Task/SetLastRun?id={id}", null, cancellationToken);
            logger.LogInformation("Successfully set last run for task: {taskId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set last run for task {taskId}: {message}", id, ex.Message);
            throw;
        }
    }
}
