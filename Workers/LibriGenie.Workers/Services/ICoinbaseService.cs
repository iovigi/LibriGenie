using LibriGenie.Workers.Services.Models;

namespace LibriGenie.Workers.Services;

public interface ICoinbaseService
{
    string GetToken(string name, string secret, string uri);
    
    Task<List<CoinbasePosition>> GetOpenPositions(string coinbaseName, string coinbasePrivateKey, CancellationToken cancellationToken);
    
    Task<decimal> GetCurrentPrice(string symbol, CancellationToken cancellationToken);
}
