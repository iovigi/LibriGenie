using LibriGenie.Workers.Services.Models;

namespace LibriGenie.Workers.Services;

public interface ICryptoReportService
{
    void TrackDailyDroppers(Dictionary<string, (List<string> events, decimal score)> cryptoEvents, Dictionary<string, CryptoMetrics> cryptoMetrics);
    System.Threading.Tasks.Task ProcessCryptoSpikeTaskAsync(
        Services.Models.Task task, 
        Dictionary<string, (List<string> events, decimal score)> cryptoEvents, 
        Dictionary<string, CryptoMetrics> cryptoMetrics, 
        CancellationToken stoppingToken);
}
