using LibriGenie.Workers.Configuration;
using LibriGenie.Workers.Services.News.Models;
using System.Net.Http.Json;

namespace LibriGenie.Workers.Services.News;

internal class NewClient(IHttpClientFactory httpClientFactory, AppSettings appSettings) : INewClient
{
    public async Task<NewResult> GetNews(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();

        return (await httpClient.GetFromJsonAsync<NewResult>(appSettings.NewUrl, cancellationToken))!;
    }
}
