using LibriGenie.Workers.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibriGenie.Workers.Services;

/// <summary>
/// Manages cryptocurrency metrics and spike detection with persistent state storage.
/// 
/// The CryptoManager maintains historical price data for cryptocurrency pairs and detects
/// significant price movements. It automatically persists its state to a JSON file
/// (crypto_state.json) to avoid re-calculating metrics on each startup.
/// 
/// Data Coverage:
/// - Always maintains exactly 2 weeks (14 days) of historical data for average calculations
/// - Automatically fills gaps in data when loading from saved state
/// - Refreshes data during initialization only if needed (data older than 2 weeks)
/// - Logs missing days when data gaps are detected
/// 
/// Absolute Min/Max Tracking:
/// - Absolute min/max values are ONLY updated by current price events in Recalculate()
/// - Data refresh operations preserve existing absolute min/max values
/// - This ensures NEW ABSOLUTE MIN/MAX events can be detected properly
/// - Historical absolute extremes are maintained across all time periods
/// 
/// State Persistence:
/// - On first run: Fetches 2 weeks of historical data from Coinbase API and saves to crypto_state.json
/// - On subsequent runs: Loads existing state from crypto_state.json, fills any gaps, and refreshes data if needed
/// - During operation: Automatically saves state when new absolute min/max values are detected
/// - Public API: Only Initialize() and Recalculate() methods are exposed
/// </summary>
public class CryptoManager : ICryptoManager
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CryptoManager> _logger;
    private readonly string _stateFilePath;

    // Static data storage for crypto metrics
    private static Dictionary<string, CryptoMetrics> _cryptoMetrics = new();
    private static readonly object _lockObject = new();
    private static bool _isInitialized = false;

    public CryptoManager(HttpClient httpClient, ILogger<CryptoManager> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LibriGenie/1.0");
        _logger = logger;

        // Set state file path in the application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _stateFilePath = Path.Combine(appDirectory, "crypto_state.json");
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
            // Try to load existing state first
            if (await LoadState())
            {
                _logger.LogInformation($"CryptoManager initialized with {_cryptoMetrics.Count} symbols from saved state");

                // Check if we need to refresh data to ensure current 2-week coverage
                await RefreshDataIfNeeded();

                lock (_lockObject)
                {
                    _isInitialized = true;
                }
                return;
            }

            // If no saved state, initialize from scratch
            await InitializeFromScratch();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initializing CryptoManager: {ex.Message}");
            throw;
        }
    }

    private async Task InitializeFromScratch()
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

        // Save the newly calculated state
        await SaveState();

        lock (_lockObject)
        {
            _isInitialized = true;
        }

        _logger.LogInformation($"CryptoManager initialized with {_cryptoMetrics.Count} symbols");
    }

    private async Task<bool> LoadState()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                _logger.LogInformation("No saved crypto state found, will initialize from scratch");
                return false;
            }

            var fileInfo = new FileInfo(_stateFilePath);
            _logger.LogInformation($"Found saved crypto state file: {_stateFilePath} (Size: {fileInfo.Length} bytes, Last modified: {fileInfo.LastWriteTime})");

            var jsonContent = await File.ReadAllTextAsync(_stateFilePath);
            var loadedMetrics = JsonSerializer.Deserialize<Dictionary<string, CryptoMetrics>>(jsonContent);

            if (loadedMetrics == null || !loadedMetrics.Any())
            {
                _logger.LogWarning("Saved crypto state is empty or invalid");
                return false;
            }

            lock (_lockObject)
            {
                _cryptoMetrics = loadedMetrics;
                
                // Migration: Initialize PreviousAbsoluteMin for existing data
                foreach (var kvp in _cryptoMetrics)
                {
                    if (kvp.Value.PreviousAbsoluteMin == 0)
                    {
                        kvp.Value.PreviousAbsoluteMin = kvp.Value.AbsoluteMin;
                        _logger.LogDebug($"Migrated PreviousAbsoluteMin for {kvp.Key}: {kvp.Value.PreviousAbsoluteMin:F8}");
                    }
                }
            }

            var oldestUpdate = _cryptoMetrics.Values.Min(m => m.LastUpdated);
            var newestUpdate = _cryptoMetrics.Values.Max(m => m.LastUpdated);

            _logger.LogInformation($"Successfully loaded crypto state for {_cryptoMetrics.Count} symbols. Data range: {oldestUpdate:yyyy-MM-dd HH:mm:ss} to {newestUpdate:yyyy-MM-dd HH:mm:ss}");

            // Check if we need to fill gaps in the data
            var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
            if (oldestUpdate > twoWeeksAgo)
            {
                _logger.LogInformation("Data is older than 2 weeks, will fill gaps to ensure complete 2-week coverage");
                await FillDataGaps();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading crypto state: {ex.Message}");
            return false;
        }
    }

    private async Task FillDataGaps()
    {
        try
        {
            _logger.LogInformation("Starting to fill data gaps for 2-week coverage...");

            var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
            var symbols = _cryptoMetrics.Keys.ToList();
            var symbolsToUpdate = new List<string>();

            // Check which symbols need updating
            foreach (var symbol in symbols)
            {
                var metrics = _cryptoMetrics[symbol];
                if (metrics.LastUpdated < twoWeeksAgo)
                {
                    symbolsToUpdate.Add(symbol);
                }
            }

            if (!symbolsToUpdate.Any())
            {
                _logger.LogInformation("All symbols have recent data (within 2 weeks), no gaps to fill");
                return;
            }

            _logger.LogInformation($"Found {symbolsToUpdate.Count} symbols that need data gap filling");

            // Update metrics for symbols with gaps
            foreach (var symbol in symbolsToUpdate)
            {
                await CalculateMetricsForSymbol(symbol);
            }

            // Save the updated state
            await SaveState();
            _logger.LogInformation($"Successfully filled data gaps for {symbolsToUpdate.Count} symbols");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error filling data gaps: {ex.Message}");
        }
    }

    private async Task SaveState()
    {
        try
        {
            Dictionary<string, CryptoMetrics> metricsToSave;

            lock (_lockObject)
            {
                metricsToSave = new Dictionary<string, CryptoMetrics>(_cryptoMetrics);
            }

            var jsonContent = JsonSerializer.Serialize(metricsToSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_stateFilePath, jsonContent);
            _logger.LogInformation($"Successfully saved crypto state for {metricsToSave.Count} symbols");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving crypto state: {ex.Message}");
        }
    }

    private async Task RefreshDataIfNeeded()
    {
        try
        {
            var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
            var symbolsToRefresh = new List<string>();

            // Check which symbols need refreshing (data older than 2 weeks)
            foreach (var kvp in _cryptoMetrics)
            {
                if (kvp.Value.LastUpdated < twoWeeksAgo)
                {
                    symbolsToRefresh.Add(kvp.Key);
                }
            }

            if (!symbolsToRefresh.Any())
            {
                _logger.LogDebug("All crypto data is current (within 2 weeks), no refresh needed");
                return;
            }

            _logger.LogInformation($"Refreshing data for {symbolsToRefresh.Count} symbols to ensure 2-week coverage");

            foreach (var symbol in symbolsToRefresh)
            {
                await CalculateMetricsForSymbol(symbol);
            }

            // Save the updated state
            await SaveState();
            _logger.LogInformation($"Successfully refreshed data for {symbolsToRefresh.Count} symbols");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error refreshing crypto data: {ex.Message}");
        }
    }

    public async Task<(Dictionary<string, (List<string> events, decimal score)> Events, Dictionary<string, CryptoMetrics> Metrics)> Recalculate()
    {
        if (!_isInitialized)
        {
            await Initialize();
        }

        var symbolEvents = new Dictionary<string, (List<string> events, decimal score)>();
        var updatedMetrics = new Dictionary<string, CryptoMetrics>();

        try
        {
            // Get current prices for all tracked symbols
            var symbols = _cryptoMetrics.Keys.ToList();

            foreach (var symbol in symbols)
            {
                var ticker = await GetCurrentTicker(symbol);
                if (ticker == null) continue;

                var metrics = _cryptoMetrics[symbol];
                var volume = ticker.Volume;
                double volumeValue = 0;
                decimal price = 0;

                if (string.IsNullOrEmpty(volume) || !double.TryParse(volume, out volumeValue) || volumeValue < 1 || !decimal.TryParse(ticker.Price, out price))
                {
                    continue; // Skip if volume is not greater than 1
                }

                var events = new List<string>();
                decimal score = 0;
                var hasEvents = false;

                // Reset daily metrics if it's a new day
                var today = DateTime.UtcNow.Date;
                var lastUpdateDate = _cryptoMetrics[symbol].LastPriceUpdate.Date;
                if (today != lastUpdateDate)
                {
                    lock (_lockObject)
                    {
                        _cryptoMetrics[symbol].DailyMin = 0;
                        _cryptoMetrics[symbol].DailyMax = 0;
                        _cryptoMetrics[symbol].DailyVolatilityCount = 0;
                        _cryptoMetrics[symbol].DailyPriceChange = 0;
                    }
                }

                // Update current price and last price update
                lock (_lockObject)
                {
                    _cryptoMetrics[symbol].CurrentPrice = price;
                    _cryptoMetrics[symbol].Volume = volumeValue;
                    _cryptoMetrics[symbol].LastPriceUpdate = DateTime.UtcNow;

                    // Update daily min/max tracking
                    if (price < _cryptoMetrics[symbol].DailyMin || _cryptoMetrics[symbol].DailyMin == 0)
                    {
                        _cryptoMetrics[symbol].DailyMin = price;
                    }
                    if (price > _cryptoMetrics[symbol].DailyMax)
                    {
                        _cryptoMetrics[symbol].DailyMax = price;
                    }

                    // Calculate daily price change
                    _cryptoMetrics[symbol].DailyPriceChange = _cryptoMetrics[symbol].DailyMax - _cryptoMetrics[symbol].DailyMin;
                }

                // Update average price tracking
                UpdateAveragePrice(symbol, price);

                // Check if we need to update 2-week averages (if data is older than 1 day)
                var shouldUpdateAverages = await ShouldUpdateAverages(symbol);
                if (shouldUpdateAverages)
                {
                    await UpdateTwoWeekAverages(symbol);
                    _logger.LogInformation($"Updated 2-week averages for {symbol}");
                }

                // Get updated metrics after potential average updates
                var updatedMetricsForSymbol = _cryptoMetrics[symbol];

                // Check for spike events with threshold tracking
                // Below average minimum logic
                if (price < updatedMetricsForSymbol.AverageMin)
                {
                    // If no stored threshold, set it and trigger event
                    if (!updatedMetricsForSymbol.StoredBelowAvgMinThreshold.HasValue)
                    {
                        lock (_lockObject)
                        {
                            _cryptoMetrics[symbol].StoredBelowAvgMinThreshold = price;
                        }
                        score += updatedMetricsForSymbol.AverageMin - price;
                        events.Add($"Price {price:F8} is below average minimum {updatedMetricsForSymbol.AverageMin:F8} - NEW THRESHOLD SET");
                        hasEvents = true;

                        lock (_lockObject)
                        {
                            if (_cryptoMetrics[symbol].IsPassedAboveAvgMaxPrevious)
                            {
                                _cryptoMetrics[symbol].DailyVolatilityCount++;
                            }

                            _cryptoMetrics[symbol].IsPassedBelowAvgMinPrevious = true;
                            _cryptoMetrics[symbol].IsPassedAboveAvgMaxPrevious = false;
                        }
                    }
                    // If price goes below the stored threshold, trigger new event
                    else if (price < updatedMetricsForSymbol.StoredBelowAvgMinThreshold.Value)
                    {
                        lock (_lockObject)
                        {
                            _cryptoMetrics[symbol].StoredBelowAvgMinThreshold = price;
                        }
                        score += updatedMetricsForSymbol.StoredBelowAvgMinThreshold.Value - price;
                        events.Add($"Price {price:F8} went below stored threshold {updatedMetricsForSymbol.StoredBelowAvgMinThreshold.Value:F8} - NEW LOW");
                        hasEvents = true;
                    }
                }
                else
                {
                    // Reset stored threshold when price is above average minimum
                    if (updatedMetricsForSymbol.StoredBelowAvgMinThreshold.HasValue)
                    {
                        lock (_lockObject)
                        {
                            _cryptoMetrics[symbol].StoredBelowAvgMinThreshold = null;
                        }
                        _logger.LogDebug($"Reset below average minimum threshold for {symbol} - price {price:F8} is above average minimum {updatedMetricsForSymbol.AverageMin:F8}");
                    }
                }

                // Above average maximum logic
                if (price > updatedMetricsForSymbol.AverageMax)
                {
                    // If no stored threshold, set it and trigger event
                    if (!updatedMetricsForSymbol.StoredAboveAvgMaxThreshold.HasValue)
                    {
                        lock (_lockObject)
                        {
                            _cryptoMetrics[symbol].StoredAboveAvgMaxThreshold = price;
                        }
                        score += price - updatedMetricsForSymbol.AverageMax;
                        events.Add($"Price {price:F8} is above average maximum {updatedMetricsForSymbol.AverageMax:F8} - NEW THRESHOLD SET");
                        hasEvents = true;

                        lock (_lockObject)
                        {
                            if (_cryptoMetrics[symbol].IsPassedBelowAvgMinPrevious)
                            {
                                _cryptoMetrics[symbol].DailyVolatilityCount++;
                            }

                            _cryptoMetrics[symbol].IsPassedAboveAvgMaxPrevious = true;
                            _cryptoMetrics[symbol].IsPassedBelowAvgMinPrevious = false;
                        }
                    }
                    // If price goes above the stored threshold, trigger new event
                    else if (price > updatedMetricsForSymbol.StoredAboveAvgMaxThreshold.Value)
                    {
                        lock (_lockObject)
                        {
                            _cryptoMetrics[symbol].StoredAboveAvgMaxThreshold = price;
                        }

                        score += price - updatedMetricsForSymbol.StoredAboveAvgMaxThreshold.Value;
                        events.Add($"Price {price:F8} went above stored threshold {updatedMetricsForSymbol.StoredAboveAvgMaxThreshold.Value:F8} - NEW HIGH");
                        hasEvents = true;
                    }
                }
                else
                {
                    // Reset stored threshold when price is below average maximum
                    if (updatedMetricsForSymbol.StoredAboveAvgMaxThreshold.HasValue)
                    {
                        lock (_lockObject)
                        {
                            _cryptoMetrics[symbol].StoredAboveAvgMaxThreshold = null;
                        }
                        _logger.LogDebug($"Reset above average maximum threshold for {symbol} - price {price:F8} is below average maximum {updatedMetricsForSymbol.AverageMax:F8}");
                    }
                }

                // Check for new absolute min/max events (these can happen with current prices)
                if (price < updatedMetricsForSymbol.AbsoluteMin)
                {
                    score += updatedMetricsForSymbol.AbsoluteMin - price;
                    events.Add($"Price {price:F8} is below absolute minimum {updatedMetricsForSymbol.AbsoluteMin:F8} - NEW ABSOLUTE MIN");
                    hasEvents = true;

                    // Update the absolute minimum and store the previous one
                    lock (_lockObject)
                    {
                        _cryptoMetrics[symbol].PreviousAbsoluteMin = _cryptoMetrics[symbol].AbsoluteMin;
                        _cryptoMetrics[symbol].AbsoluteMin = price;
                    }
                }

                if (price > updatedMetricsForSymbol.AbsoluteMax)
                {
                    score += price - updatedMetricsForSymbol.AbsoluteMax;
                    events.Add($"Price {price:F8} is above absolute maximum {updatedMetricsForSymbol.AbsoluteMax:F8} - NEW ABSOLUTE MAX");
                    hasEvents = true;

                    // Update the absolute maximum
                    lock (_lockObject)
                    {
                        _cryptoMetrics[symbol].AbsoluteMax = price;
                    }
                }

                // Always add metrics to the result, regardless of events
                lock (_lockObject)
                {
                    updatedMetrics[symbol] = new CryptoMetrics
                    {
                        Symbol = _cryptoMetrics[symbol].Symbol,
                        AverageMin = _cryptoMetrics[symbol].AverageMin,
                        AverageMax = _cryptoMetrics[symbol].AverageMax,
                        AbsoluteMin = _cryptoMetrics[symbol].AbsoluteMin,
                        AbsoluteMax = _cryptoMetrics[symbol].AbsoluteMax,
                        PreviousAbsoluteMin = _cryptoMetrics[symbol].PreviousAbsoluteMin,
                        LastUpdated = _cryptoMetrics[symbol].LastUpdated,
                        CurrentPrice = _cryptoMetrics[symbol].CurrentPrice,
                        Volume = _cryptoMetrics[symbol].Volume,
                        AveragePrice = _cryptoMetrics[symbol].AveragePrice,
                        LastPriceUpdate = _cryptoMetrics[symbol].LastPriceUpdate,
                        LastAverageUpdate = _cryptoMetrics[symbol].LastAverageUpdate,
                        DailyPriceCount = _cryptoMetrics[symbol].DailyPriceCount,
                        DailyPriceSum = _cryptoMetrics[symbol].DailyPriceSum,
                        StoredBelowAvgMinThreshold = _cryptoMetrics[symbol].StoredBelowAvgMinThreshold,
                        StoredAboveAvgMaxThreshold = _cryptoMetrics[symbol].StoredAboveAvgMaxThreshold,
                        DailyMin = _cryptoMetrics[symbol].DailyMin,
                        DailyMax = _cryptoMetrics[symbol].DailyMax,
                        DailyVolatilityCount = _cryptoMetrics[symbol].DailyVolatilityCount,
                        DailyPriceChange = _cryptoMetrics[symbol].DailyPriceChange,
                        IsPassedBelowAvgMinPrevious = _cryptoMetrics[symbol].IsPassedBelowAvgMinPrevious,
                        IsPassedAboveAvgMaxPrevious = _cryptoMetrics[symbol].IsPassedAboveAvgMaxPrevious
                    };
                }

                if (hasEvents)
                {
                    symbolEvents[symbol] = (events, score);
                }
            }

            // Save state after recalculating (in case absolute min/max values or averages changed)
            if (symbolEvents.Any())
            {
                await SaveState();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in Recalculate: {ex.Message}");
        }

        return (symbolEvents, updatedMetrics);
    }

    private async Task CalculateMetricsForSymbol(string symbol)
    {
        try
        {
            var granularity = 300; // 5-minute candles
            var maxCandlesPerRequest = 300;
            var maxTimeSpan = TimeSpan.FromSeconds(granularity * maxCandlesPerRequest); // 25 hours

            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-14);

            var allPrices = new List<decimal>();
            var allCandles = new List<List<decimal>>(); // Store full candles for date grouping

            var currentStart = startTime;

            while (currentStart < endTime)
            {
                var currentEnd = currentStart + maxTimeSpan;
                if (currentEnd > endTime)
                    currentEnd = endTime;

                var url = $"https://api.exchange.coinbase.com/products/{symbol}/candles" +
                          $"?start={currentStart:yyyy-MM-ddTHH:mm:ssZ}" +
                          $"&end={currentEnd:yyyy-MM-ddTHH:mm:ssZ}" +
                          $"&granularity={granularity}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("CryptoMetricsApp/1.0");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to fetch data for {symbol} from {currentStart} to {currentEnd}. Status: {response.StatusCode}. Response: {error}");
                    currentStart = currentEnd;
                    continue;
                }

                var candlesJson = await response.Content.ReadAsStringAsync();
                var candles = JsonSerializer.Deserialize<List<List<decimal>>>(candlesJson);

                if (candles != null && candles.Any())
                {
                    allCandles.AddRange(candles);
                    allPrices.AddRange(candles.Select(c => c[4])); // close prices
                }

                currentStart = currentEnd;
            }

            if (!allPrices.Any())
            {
                _logger.LogInformation($"No price data collected for {symbol}.");
                return;
            }

            // Group prices by day using candle timestamps
            var pricesByDay = new Dictionary<DateTime, List<decimal>>();

            foreach (var candle in allCandles)
            {
                var timestamp = DateTimeOffset.FromUnixTimeSeconds((long)candle[0]).UtcDateTime;
                var closePrice = candle[4];

                var day = timestamp.Date;

                if (!pricesByDay.ContainsKey(day))
                    pricesByDay[day] = new List<decimal>();

                pricesByDay[day].Add(closePrice);
            }

            // Check for gaps in daily data and log them
            var expectedDays = Enumerable.Range(0, 14)
                .Select(i => startTime.Date.AddDays(i))
                .ToList();

            var missingDays = expectedDays.Where(day => !pricesByDay.ContainsKey(day)).ToList();
            if (missingDays.Any())
            {
                _logger.LogWarning($"Missing data for {symbol} on {missingDays.Count} days: {string.Join(", ", missingDays.Select(d => d.ToString("yyyy-MM-dd")))}");
            }

            // Compute daily min/max and their averages
            var dailyMins = pricesByDay.Values.Select(prices => prices.Min()).ToList();
            var dailyMaxs = pricesByDay.Values.Select(prices => prices.Max()).ToList();

            var averageMin = dailyMins.Average();
            var averageMax = dailyMaxs.Average();

            // Preserve historical absolute min/max values - they should only be updated by current price events
            decimal absoluteMin = allPrices.Min();
            decimal absoluteMax = allPrices.Max();

            lock (_lockObject)
            {
                if (_cryptoMetrics.ContainsKey(symbol))
                {
                    var existingMetrics = _cryptoMetrics[symbol];

                    // Preserve existing absolute min/max values (they should only be updated by current price events)
                    absoluteMin = existingMetrics.AbsoluteMin;
                    absoluteMax = existingMetrics.AbsoluteMax;

                    _logger.LogInformation($"Preserved absolute min/max for {symbol}: {absoluteMin:F8} - {absoluteMax:F8} (2-week range: {allPrices.Min():F8} - {allPrices.Max():F8})");
                }
            }

            var metrics = new CryptoMetrics
            {
                Symbol = symbol,
                AverageMin = averageMin,
                AverageMax = averageMax,
                AbsoluteMin = absoluteMin,
                AbsoluteMax = absoluteMax,
                PreviousAbsoluteMin = absoluteMin, // Initialize to same value as absolute min
                LastUpdated = DateTime.UtcNow,
                CurrentPrice = 0,
                Volume = 0,
                AveragePrice = 0,
                LastPriceUpdate = DateTime.UtcNow,
                LastAverageUpdate = DateTime.UtcNow,
                DailyPriceCount = 0,
                DailyPriceSum = 0,
                StoredBelowAvgMinThreshold = null,
                StoredAboveAvgMaxThreshold = null,
                DailyMin = 0,
                DailyMax = 0,
                DailyVolatilityCount = 0,
                DailyPriceChange = 0
            };

            lock (_lockObject)
            {
                _cryptoMetrics[symbol] = metrics;
            }

            _logger.LogInformation($"Updated metrics for {symbol}: {pricesByDay.Count} days of data, {allPrices.Count} price points. Absolute range: {absoluteMin:F8} - {absoluteMax:F8}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calculating metrics for {symbol}: {ex.Message}");
        }
    }

    private async Task<CoinbaseTicker?> GetCurrentTicker(string symbol)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.exchange.coinbase.com/products/{symbol}/ticker");
            response.EnsureSuccessStatusCode();

            var tickerJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<CoinbaseTicker>(tickerJson);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting ticker for {symbol}: {ex.Message}");
            return null;
        }
    }

    private void UpdateAveragePrice(string symbol, decimal currentPrice)
    {
        try
        {
            lock (_lockObject)
            {
                var metrics = _cryptoMetrics[symbol];
                var today = DateTime.UtcNow.Date;
                var lastUpdateDate = metrics.LastAverageUpdate.Date;

                // If it's a new day, reset the daily counters
                if (lastUpdateDate != today)
                {
                    metrics.DailyPriceCount = 0;
                    metrics.DailyPriceSum = 0;
                    _logger.LogDebug($"Reset daily price counters for {symbol} - new day detected");
                }

                // Add current price to daily sum and increment count
                metrics.DailyPriceSum += currentPrice;
                metrics.DailyPriceCount++;

                // Calculate new average price
                if (metrics.DailyPriceCount > 0)
                {
                    metrics.AveragePrice = metrics.DailyPriceSum / metrics.DailyPriceCount;
                }

                metrics.LastAverageUpdate = DateTime.UtcNow;

                _logger.LogDebug($"Updated average price for {symbol}: Current={currentPrice:F8}, Daily Avg={metrics.AveragePrice:F8}, Count={metrics.DailyPriceCount}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating average price for {symbol}: {ex.Message}");
        }
    }

    private async Task<bool> ShouldUpdateAverages(string symbol)
    {
        try
        {
            lock (_lockObject)
            {
                var metrics = _cryptoMetrics[symbol];
                var oneDayAgo = DateTime.UtcNow.AddDays(-1);

                // Update averages if the last update was more than 1 day ago
                return metrics.LastUpdated < oneDayAgo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking if averages should be updated for {symbol}: {ex.Message}");
            return false;
        }
    }

    private async Task UpdateTwoWeekAverages(string symbol)
    {
        try
        {
            var granularity = 300; // 5-minute candles
            var maxCandlesPerRequest = 300;
            var maxTimeSpan = TimeSpan.FromSeconds(granularity * maxCandlesPerRequest); // 25 hours

            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-14);

            var allPrices = new List<decimal>();
            var allCandles = new List<List<decimal>>(); // Store full candles for date grouping

            var currentStart = startTime;

            while (currentStart < endTime)
            {
                var currentEnd = currentStart + maxTimeSpan;
                if (currentEnd > endTime)
                    currentEnd = endTime;

                var url = $"https://api.exchange.coinbase.com/products/{symbol}/candles" +
                          $"?start={currentStart:yyyy-MM-ddTHH:mm:ssZ}" +
                          $"&end={currentEnd:yyyy-MM-ddTHH:mm:ssZ}" +
                          $"&granularity={granularity}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("CryptoMetricsApp/1.0");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to fetch data for {symbol} from {currentStart} to {currentEnd}. Status: {response.StatusCode}. Response: {error}");
                    currentStart = currentEnd;
                    continue;
                }

                var candlesJson = await response.Content.ReadAsStringAsync();
                var candles = JsonSerializer.Deserialize<List<List<decimal>>>(candlesJson);

                if (candles != null && candles.Any())
                {
                    allCandles.AddRange(candles);
                    allPrices.AddRange(candles.Select(c => c[4])); // close prices
                }

                currentStart = currentEnd;
            }

            if (!allPrices.Any())
            {
                _logger.LogWarning($"No price data collected for {symbol} during average update.");
                return;
            }

            // Group prices by day using candle timestamps
            var pricesByDay = new Dictionary<DateTime, List<decimal>>();

            foreach (var candle in allCandles)
            {
                var timestamp = DateTimeOffset.FromUnixTimeSeconds((long)candle[0]).UtcDateTime;
                var closePrice = candle[4];

                var day = timestamp.Date;

                if (!pricesByDay.ContainsKey(day))
                    pricesByDay[day] = new List<decimal>();

                pricesByDay[day].Add(closePrice);
            }

            // Compute daily min/max and their averages
            var dailyMins = pricesByDay.Values.Select(prices => prices.Min()).ToList();
            var dailyMaxs = pricesByDay.Values.Select(prices => prices.Max()).ToList();

            var averageMin = dailyMins.Average();
            var averageMax = dailyMaxs.Average();

            // Update the averages in the metrics
            lock (_lockObject)
            {
                _cryptoMetrics[symbol].AverageMin = averageMin;
                _cryptoMetrics[symbol].AverageMax = averageMax;
                _cryptoMetrics[symbol].LastUpdated = DateTime.UtcNow;
            }

            _logger.LogInformation($"Updated 2-week averages for {symbol}: AverageMin={averageMin:F8}, AverageMax={averageMax:F8} (from {pricesByDay.Count} days)");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating 2-week averages for {symbol}: {ex.Message}");
        }
    }

    public async Task<List<Models.ShortTermInvestmentOpportunity>> AnalyzeShortTermInvestmentOpportunities(Dictionary<string, CryptoMetrics> cryptoMetrics, CancellationToken stoppingToken)
    {
        var opportunities = new List<Models.ShortTermInvestmentOpportunity>();

        try
        {
            foreach (var kvp in cryptoMetrics)
            {
                var symbol = kvp.Key;
                var metrics = kvp.Value;

                // Skip if we don't have enough data
                if (metrics.CurrentPrice <= 0 || metrics.DailyMin <= 0 || metrics.DailyMax <= 0)
                    continue;

                // Calculate Coinbase fees for trading
                var buyFee = 1.5m; // 1.5 EUR fixed fee
                var sellFee = 1.5m; // 1.5 EUR fixed fee
                var priceFee = metrics.CurrentPrice * 0.0001m; // 0.01% of price
                var totalFees = buyFee + sellFee + priceFee;

                // Calculate daily opportunity scenarios using daily average price for selling
                var currentPrice = metrics.CurrentPrice;
                var dailyMin = metrics.DailyMin;
                var dailyMax = metrics.DailyMax;
                var dailyAverage = metrics.AveragePrice; // Use daily average price for selling
                var dailyRange = dailyMax - dailyMin;

                // Skip if we don't have daily average price data
                if (dailyAverage <= 0)
                    continue;

                // Only scenario: Buy at current price, sell at daily average
                var profitAtDailyAverage = dailyAverage - currentPrice - totalFees;
                var profitPercentageAtDailyAverage = currentPrice > 0 ? (profitAtDailyAverage / currentPrice) * 100 : 0;

                // Calculate investment amounts for 100 EUR limit check
                var investmentAmountCurrentToAverage = currentPrice > 0 ? 100m / currentPrice : 0;

                // Calculate actual profit amounts for the scenario
                // For €100 investment: (sellPrice - buyPrice) * coinAmount - totalFees
                var actualProfitCurrentToAverage = investmentAmountCurrentToAverage * (dailyAverage - currentPrice) - totalFees;

                // Calculate risk metrics
                var currentToMinDistance = currentPrice - dailyMin;
                var currentToMaxDistance = dailyMax - currentPrice;
                var riskRewardRatio = dailyRange > 0 ? Math.Abs(currentToMinDistance / dailyRange) : 0;

                // Determine opportunity type and score
                var opportunityType = "NONE";
                var opportunityScore = 0m;
                var recommendation = "";
                var actualProfit = 0m;
                var investmentAmount = 0m;

                if (profitAtDailyAverage > 0 && profitPercentageAtDailyAverage > 1 && actualProfitCurrentToAverage > 0) // At least 1% profit and positive actual profit
                {
                    opportunityType = "BUY_CURRENT_SELL_DAILY_AVERAGE";
                    opportunityScore = profitPercentageAtDailyAverage;
                    actualProfit = actualProfitCurrentToAverage;
                    investmentAmount = investmentAmountCurrentToAverage;
                    recommendation = $"Buy at current price {currentPrice:F8}, target sell at daily average {dailyAverage:F8} for {profitPercentageAtDailyAverage:F2}% profit (€{actualProfit:F2} profit on €100 investment)";
                }

                // Only include opportunities with meaningful profit potential and positive actual profit
                if (opportunityScore > 1 && actualProfit > 0)
                {
                    opportunities.Add(new Models.ShortTermInvestmentOpportunity
                    {
                        Symbol = symbol,
                        OpportunityType = opportunityType,
                        CurrentPrice = currentPrice,
                        AverageMin = dailyMin, // Using daily min
                        AverageMax = dailyAverage, // Using daily average price instead of daily max
                        TwoWeekRange = dailyRange, // Using daily range
                        TotalFees = totalFees,
                        ProfitAtAvg = actualProfitCurrentToAverage, // Actual profit when selling at daily average
                        ProfitPercentageAtAvg = profitPercentageAtDailyAverage, // Percentage profit when selling at daily average
                        ProfitAtAvgFromMin = 0, // Not used - no daily min scenarios
                        ProfitPercentageAtAvgFromMin = 0, // Not used - no daily min scenarios
                        ProfitAtFullRange = actualProfitCurrentToAverage, // Same as main scenario
                        ProfitPercentageAtFullRange = profitPercentageAtDailyAverage, // Same as main scenario
                        RiskRewardRatio = riskRewardRatio,
                        Recommendation = recommendation,
                        OpportunityScore = opportunityScore,
                        Volume = metrics.Volume,
                        DailyVolatilityCount = metrics.DailyVolatilityCount
                    });
                }
            }

            // Sort by opportunity score (highest first)
            opportunities = opportunities.OrderByDescending(o => o.OpportunityScore).ToList();

            _logger.LogInformation("Analyzed {count} symbols for daily investment opportunities (€100 max, selling at daily average), found {opportunities} with potential", 
                cryptoMetrics.Count, opportunities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing daily investment opportunities: {message}", ex.Message);
        }

        return opportunities;
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
    public decimal PreviousAbsoluteMin { get; set; } // Track previous absolute minimum
    public DateTime LastUpdated { get; set; }
    // New fields for average price tracking
    public decimal CurrentPrice { get; set; }
    public double Volume { get; set; }
    public decimal AveragePrice { get; set; }
    public DateTime LastPriceUpdate { get; set; }
    public DateTime LastAverageUpdate { get; set; }
    public int DailyPriceCount { get; set; }
    public decimal DailyPriceSum { get; set; }
    // New fields for threshold tracking
    public decimal? StoredBelowAvgMinThreshold { get; set; }
    public decimal? StoredAboveAvgMaxThreshold { get; set; }
    // New fields for daily volatility tracking
    public decimal DailyMin { get; set; }
    public decimal DailyMax { get; set; }
    public bool IsPassedBelowAvgMinPrevious { get; set; }
    public bool IsPassedAboveAvgMaxPrevious { get; set; }
    public int DailyVolatilityCount { get; set; } // Count of times price moved between below avg min and above avg max
    public decimal DailyPriceChange { get; set; } // Daily price change from min to max
}

public class CoinbaseProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("quote_currency")]
    public string QuoteCurrency { get; set; } = string.Empty;
}

public class CoinbaseTicker
{
    [JsonPropertyName("price")]
    public string Price { get; set; }
    [JsonPropertyName("volume")]
    public string Volume { get; set; }
}