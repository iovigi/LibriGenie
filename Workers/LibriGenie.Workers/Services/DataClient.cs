using LibriGenie.Workers.Configuration;
using LibriGenie.Workers.Services.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace LibriGenie.Workers.Services;

public class DataClient(HttpClient httpClient, AppSettings appSettings) : IDataClient
{
    public async Task<InsertResponse> InsertAsync<T>(string typeName, T data, CancellationToken cancellationToken = default)
    {
        // Set headers as per the API specification
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("X-System-Auth", appSettings.DataClientSettings.SystemAuthKey);
        httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

        // Create the request body with the data wrapper
        var requestBody = new { data };
        
        var response = await httpClient.PostAsJsonAsync($"{appSettings.DataClientSettings.BaseUrl}insert?type={typeName}", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<InsertResponse>(cancellationToken: cancellationToken) 
            ?? throw new InvalidOperationException($"Failed to deserialize response for {typeName}");
    }

    public async Task<QueryResponse<T>> GetAllAsync<T>(string typeName, string field = "", string value = "", int page = 1, int perPage = 10, string orderDirection = "asc", CancellationToken cancellationToken = default)
    {
        // Set headers as per the API specification
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("X-System-Auth", appSettings.DataClientSettings.SystemAuthKey);

        var queryParams = new List<string>
        {
            $"type={Uri.EscapeDataString(typeName)}",
            $"page={page}",
            $"per_page={perPage}",
            $"order_direction={orderDirection}"
        };

        if (!string.IsNullOrEmpty(field))
        {
            queryParams.Add($"field={Uri.EscapeDataString(field)}");
        }

        if (!string.IsNullOrEmpty(value))
        {
            queryParams.Add($"value={Uri.EscapeDataString(value)}");
        }

        var url = $"{appSettings.DataClientSettings.BaseUrl}query?{string.Join("&", queryParams)}";

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<QueryResponse<T>>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize response for {typeName}");
    }
} 