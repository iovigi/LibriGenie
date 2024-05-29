using LibriGenie.Workers.Configuration;
using System.Net.Http.Json;

namespace LibriGenie.Workers.Services;

public class LibriGenieClient(HttpClient httpClient, AppSettings appSettings) : ILibriGenieClient
{
    public async Task<List<Models.Task>> GetTasksForRun(int page, int pageSize,CancellationToken cancellationToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = appSettings.ApiConfiguration.GetAuthenticationHeaderValue();

        return (await httpClient.GetFromJsonAsync<List<Models.Task>>($"{appSettings.ApiConfiguration.Endpoint}Task/GetTasksForRun?page={page}&pageSize={pageSize}", cancellationToken))!;
    }

    public async System.Threading.Tasks.Task SetLastRun(string id, CancellationToken cancellationToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = appSettings.ApiConfiguration.GetAuthenticationHeaderValue();

        await httpClient.PostAsync($"{appSettings.ApiConfiguration.Endpoint}Task/SetLastRun?id={id}", null, cancellationToken);
    }
}
