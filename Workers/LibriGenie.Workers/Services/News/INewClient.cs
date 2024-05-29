using LibriGenie.Workers.Services.News.Models;

namespace LibriGenie.Workers.Services.News;

public interface INewClient
{
    Task<NewResult> GetNews(CancellationToken cancellationToken);
}
