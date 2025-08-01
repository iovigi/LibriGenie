using LibriGenie.Workers.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

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

    public async Task SendTextFromNoReply(string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            var emailRequest = new
            {
                To = to,
                Subject = subject,
                Body = body
            };

            var json = JsonSerializer.Serialize(emailRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Authorization = appSettings.ApiConfiguration.GetAuthenticationHeaderValue();

            var response = await httpClient.PostAsync($"{appSettings.ApiConfiguration.Endpoint}Email/Send", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to send email via API. Status: {status}, Error: {error}",
                    response.StatusCode, errorContent);
                throw new Exception($"Failed to send email via API. Status: {response.StatusCode}, Error: {errorContent}");
            }

            logger.LogInformation("Email sent successfully via API to {to}", to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email via API to {to}: {message}", to, ex.Message);
            throw;
        }
    }
}
