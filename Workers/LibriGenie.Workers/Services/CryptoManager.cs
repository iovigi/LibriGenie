using LibriGenie.Workers.Configuration;
using LibriGenie.Workers.Services.Brevo;
using System.Text.Json;

namespace LibriGenie.Workers.Services;

public class CryptoManager : ICryptoManager
{
    private readonly HttpClient _httpClient;
    
    // Static data storage for crypto metrics
    private static Dictionary<string, CryptoMetrics> _cryptoMetrics = new();
    private static readonly object _lockObject = new();
    private static bool _isInitialized = false;

    public CryptoManager(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task Initialize()
    {
        if (_isInitialized) return;

        lock (_lockObject)
        {
            if (_isInitialized) return;
        }

        try
        {
            // Get all products from Coinbase
            var response = await _httpClient.GetAsync("https://api.exchange.coinbase.com/products");
            response.EnsureSuccessStatusCode();
            
            var productsJson = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<CoinbaseProduct>>(productsJson);

            if (products == null) return;

            // Filter for USD and EUR pairs
            var usdEurProducts = products.Where(p => 
                p.Status == "online" && 
                (p.QuoteCurrency == "USD" || p.QuoteCurrency == "EUR")).ToList();

            foreach (var product in usdEurProducts)
            {
                await CalculateMetricsForSymbol(product.Id);
            }

            lock (_lockObject)
            {
                _isInitialized = true;
            }

            Console.WriteLine($"CryptoManager initialized with {_cryptoMetrics.Count} symbols");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing CryptoManager: {ex.Message}");
            throw;
        }
    }

    public async Task<Dictionary<string, List<string>>> Recalculate()
    {
        if (!_isInitialized)
        {
            await Initialize();
        }

        var symbolEvents = new Dictionary<string, List<string>>();

        try
        {
            // Get current prices for all tracked symbols
            var symbols = _cryptoMetrics.Keys.ToList();
            
            foreach (var symbol in symbols)
            {
                var currentPrice = await GetCurrentPrice(symbol);
                if (currentPrice == null) continue;

                var metrics = _cryptoMetrics[symbol];
                var volume = await GetCurrentVolume(symbol);
                
                if (volume <= 1) continue; // Skip if volume is not greater than 1

                var events = new List<string>();

                // Check for spike events
                if (currentPrice < metrics.AverageMin)
                {
                    events.Add($"Price {currentPrice:F8} is below average minimum {metrics.AverageMin:F8}");
                }
                
                if (currentPrice > metrics.AverageMax)
                {
                    events.Add($"Price {currentPrice:F8} is above average maximum {metrics.AverageMax:F8}");
                }

                if (currentPrice < metrics.AbsoluteMin)
                {
                    events.Add($"Price {currentPrice:F8} is below absolute minimum {metrics.AbsoluteMin:F8} - NEW ABSOLUTE MIN");
                    metrics.AbsoluteMin = currentPrice.Value;
                }

                if (currentPrice > metrics.AbsoluteMax)
                {
                    events.Add($"Price {currentPrice:F8} is above absolute maximum {metrics.AbsoluteMax:F8} - NEW ABSOLUTE MAX");
                    metrics.AbsoluteMax = currentPrice.Value;
                }

                if (events.Any())
                {
                    symbolEvents[symbol] = events;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Recalculate: {ex.Message}");
        }

        return symbolEvents;
    }

    private async Task CalculateMetricsForSymbol(string symbol)
    {
        try
        {
            // Get 2 weeks of historical data (14 days * 24 hours = 336 candles)
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-14);
            
            var url = $"https://api.exchange.coinbase.com/products/{symbol}/candles?start={startTime:yyyy-MM-ddTHH:mm:ssZ}&end={endTime:yyyy-MM-ddTHH:mm:ssZ}&granularity=3600"; // 1-hour candles
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var candlesJson = await response.Content.ReadAsStringAsync();
            var candles = JsonSerializer.Deserialize<List<List<decimal>>>(candlesJson);

            if (candles == null || !candles.Any()) return;

            var prices = candles.Select(c => c[4]).ToList(); // Close prices
            
            var metrics = new CryptoMetrics
            {
                Symbol = symbol,
                AverageMin = prices.Average(),
                AverageMax = prices.Average(),
                AbsoluteMin = prices.Min(),
                AbsoluteMax = prices.Max(),
                LastUpdated = DateTime.UtcNow
            };

            lock (_lockObject)
            {
                _cryptoMetrics[symbol] = metrics;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating metrics for {symbol}: {ex.Message}");
        }
    }

    private async Task<decimal?> GetCurrentPrice(string symbol)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.exchange.coinbase.com/products/{symbol}/ticker");
            response.EnsureSuccessStatusCode();
            
            var tickerJson = await response.Content.ReadAsStringAsync();
            var ticker = JsonSerializer.Deserialize<CoinbaseTicker>(tickerJson);
            
            return ticker?.Price;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current price for {symbol}: {ex.Message}");
            return null;
        }
    }

    private async Task<decimal> GetCurrentVolume(string symbol)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.exchange.coinbase.com/products/{symbol}/ticker");
            response.EnsureSuccessStatusCode();
            
            var tickerJson = await response.Content.ReadAsStringAsync();
            var ticker = JsonSerializer.Deserialize<CoinbaseTicker>(tickerJson);
            
            return ticker?.Volume ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting volume for {symbol}: {ex.Message}");
            return 0;
        }
    }


}

// Supporting classes
public class CryptoMetrics
{
    public string Symbol { get; set; } = string.Empty;
    public decimal AverageMin { get; set; }
    public decimal AverageMax { get; set; }
    public decimal AbsoluteMin { get; set; }
    public decimal AbsoluteMax { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CoinbaseProduct
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
}

public class CoinbaseTicker
{
    public decimal? Price { get; set; }
    public decimal? Volume { get; set; }
} 