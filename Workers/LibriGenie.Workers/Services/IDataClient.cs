using LibriGenie.Workers.Services.Models;

namespace LibriGenie.Workers.Services;

public interface IDataClient
{
    Task<InsertResponse> InsertAsync<T>(string typeName, T data, CancellationToken cancellationToken = default);
    Task<QueryResponse<T>> GetAllAsync<T>(string typeName, string field = "", string value = "", int page = 1, int perPage = 10, string orderDirection = "asc", CancellationToken cancellationToken = default);
} 