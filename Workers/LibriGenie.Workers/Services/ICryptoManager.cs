using LibriGenie.Workers.Services.Models;

namespace LibriGenie.Workers.Services;

public interface ICryptoManager
{
    System.Threading.Tasks.Task Initialize();
    Task<(Dictionary<string, (List<string> events, decimal score)> Events, Dictionary<string, CryptoMetrics> Metrics)> Recalculate();
    Task<List<ShortTermInvestmentOpportunity>> AnalyzeShortTermInvestmentOpportunities(Dictionary<string, CryptoMetrics> cryptoMetrics, CancellationToken stoppingToken);
} 