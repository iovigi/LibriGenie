using LibriGenie.Workers.Services;
using LibriGenie.Workers.Services.ContentGenerate;
using System.Text.Json;

namespace LibriGenie.Workers;

public class Worker(ILibriGenieClient libriGenieClient, IWordpressPublisher wordpressPublisher, IContentGenerator contentGenerator, ICryptoManager cryptoManager, IMailService mailService, ILogger<Worker> logger) : BackgroundService
{
    // Static field to track daily droppers (symbols that hit new absolute minimums)
    private static Dictionary<string, (decimal PreviousMin, decimal CurrentMin, DateTime TimeStamp, decimal CurrentPrice, decimal AvgPrice)> _dailyDroppers = new();
    private static DateTime _lastDailyReset = DateTime.UtcNow.Date;
    private void ResetDailyDroppersIfNeeded()
    {
        var currentDate = DateTime.UtcNow.Date;
        if (currentDate > _lastDailyReset)
        {
            _dailyDroppers.Clear();
            _lastDailyReset = currentDate;
            logger.LogInformation("Daily droppers reset for new day: {date}", currentDate);
        }
    }

    private void TrackDailyDropper(string symbol, decimal previousMin, decimal currentMin, decimal currentPrice, decimal avgPrice)
    {
        ResetDailyDroppersIfNeeded();
        _dailyDroppers[symbol] = (previousMin, currentMin, DateTime.UtcNow, currentPrice, avgPrice);
        logger.LogInformation("Tracked daily dropper: {symbol} - Previous: {previousMin:F8}, Current: {currentMin:F8}", 
            symbol, previousMin, currentMin);
    }

    private List<Services.Models.Task> GetTasksForRunFromBackup(int page, int pageSize)
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "active_tasks.json");

            if (!File.Exists(filePath))
            {
                logger.LogWarning("Backup file active_tasks.json not found");
                return new List<Services.Models.Task>();
            }

            var jsonContent = File.ReadAllText(filePath);
            var allTasks = JsonSerializer.Deserialize<List<Services.Models.Task>>(jsonContent);

            if (allTasks == null || !allTasks.Any())
            {
                logger.LogWarning("No tasks found in backup file or file is empty");
                return new List<Services.Models.Task>();
            }

            // Apply the same filtering logic as the API's GetTasksForRun
            var utcNow = DateTime.UtcNow;
            var timeOfDay = utcNow.TimeOfDay;
            var timePlusFive = timeOfDay.Add(TimeSpan.FromMinutes(5));

            var filteredTasks = allTasks.Where(task =>
                (task.LastRun == null || task.LastRun.Value.Date != utcNow.Date) &&
                (task.Time <= timePlusFive)
            ).ToList();

            // Apply pagination
            var paginatedTasks = filteredTasks
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            logger.LogInformation("Retrieved {count} tasks from backup file (page {page}, pageSize {pageSize})",
                paginatedTasks.Count, page, pageSize);

            return paginatedTasks;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read tasks from backup file: {message}", ex.Message);
            return new List<Services.Models.Task>();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize CryptoManager on startup
        try
        {
            await cryptoManager.Initialize();
            logger.LogInformation("CryptoManager initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize CryptoManager: {message}", ex.Message);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            try
            {
                // Run crypto spike detection
                Dictionary<string, (List<string> events, decimal score)> cryptoEvents = new Dictionary<string, (List<string> events, decimal score)>();
                Dictionary<string, CryptoMetrics> cryptoMetrics = new Dictionary<string, CryptoMetrics>();
                try
                {
                    var result = await cryptoManager.Recalculate();
                    cryptoEvents = result.Events;
                    cryptoMetrics = result.Metrics;

                    // Track daily droppers (symbols that hit new absolute minimums)
                    foreach (var kvp in cryptoEvents)
                    {
                        var symbol = kvp.Key;
                        var events = kvp.Value.events;
                        
                        // Check if any event is about new absolute minimum
                        var newAbsoluteMinEvent = events.FirstOrDefault(e => e.Contains("NEW ABSOLUTE MIN"));
                        if (!string.IsNullOrEmpty(newAbsoluteMinEvent) && cryptoMetrics.ContainsKey(symbol))
                        {
                            var metrics = cryptoMetrics[symbol];
                            TrackDailyDropper(symbol, metrics.PreviousAbsoluteMin, metrics.AbsoluteMin, metrics.CurrentPrice, metrics.AveragePrice);
                        }
                    }

                    // Remove symbols from daily droppers if current price is higher than average minimum
                    var symbolsToRemove = new List<string>();
                    foreach (var kvp in _dailyDroppers)
                    {
                        var symbol = kvp.Key;
                        if (cryptoMetrics.ContainsKey(symbol))
                        {
                            var metrics = cryptoMetrics[symbol];
                            if (metrics.CurrentPrice > metrics.AverageMin)
                            {
                                symbolsToRemove.Add(symbol);
                                logger.LogInformation("Removed {symbol} from daily droppers - current price {currentPrice:F8} is above average min {avgMin:F8}", 
                                    symbol, metrics.CurrentPrice, metrics.AverageMin);
                            }
                        }
                    }

                    foreach (var symbol in symbolsToRemove)
                    {
                        _dailyDroppers.Remove(symbol);
                    }

                    if (cryptoEvents.Any())
                    {
                        logger.LogInformation("Crypto spike events detected for {count} symbols", cryptoEvents.Count);
                    }

                    if (cryptoMetrics.Any())
                    {
                        logger.LogInformation("Crypto metrics updated for {count} symbols", cryptoMetrics.Count);
                    }
                }
                catch (Exception cryptoEx)
                {
                    logger.LogError(cryptoEx, "Error in crypto spike detection: {message}", cryptoEx.Message);
                }
                int page_size = 100;
                int page = 0;
                List<Services.Models.Task> tasks = new List<Services.Models.Task>();

                do
                {
                    try
                    {
                        // Try to get tasks from API first
                        tasks = await libriGenieClient.GetTasksForRun(page, page_size, stoppingToken);
                        logger.LogInformation("Successfully retrieved tasks from API (page {page})", page);
                    }
                    catch (Exception apiEx)
                    {
                        logger.LogWarning(apiEx, "Failed to get tasks from API, using backup file: {message}", apiEx.Message);

                        // Fallback to backup file
                        tasks = GetTasksForRunFromBackup(page, page_size);

                        if (!tasks.Any())
                        {
                            logger.LogWarning("No tasks available from backup file, skipping this cycle");
                            break;
                        }
                    }

                    foreach (var task in tasks)
                    {
                        try
                        {
                            if (task.Category == "CryptoSpikes")
                            {
                                // Handle crypto spike tasks
                                await ProcessCryptoSpikeTask(task, cryptoEvents, cryptoMetrics, stoppingToken);
                            }
                            else
                            {
                                // Handle regular content generation tasks
                                var content = await contentGenerator.Generate(task.Category, stoppingToken);
                                await mailService.Send(task.Email, content.Title, content.Content, stoppingToken);

                                if (task.EnableWordpress)
                                {
                                    await wordpressPublisher.Publish(task, content.Title, content.Content, stoppingToken);
                                }
                            }

                            // Only try to update last run if we got tasks from API
                            try
                            {
                                await libriGenieClient.SetLastRun(task.Id, stoppingToken);
                            }
                            catch (Exception setLastRunEx)
                            {
                                logger.LogWarning(setLastRunEx, "Failed to update last run for task {taskId}: {message}", task.Id, setLastRunEx.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                        }
                    }
                }
                while (tasks.Count == page_size);

                // After processing all tasks, get all active tasks and save to JSON file
                try
                {
                    var allActiveTasks = await libriGenieClient.GetAllActiveTasks(stoppingToken);
                    var jsonContent = JsonSerializer.Serialize(allActiveTasks, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    var fileName = "active_tasks.json";
                    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                    await File.WriteAllTextAsync(filePath, jsonContent, stoppingToken);

                    logger.LogInformation("All active tasks saved to file: {fileName}", fileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to save active tasks to JSON file: {message}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

            await Task.Delay(1000 * 60 * 5, stoppingToken);
        }
    }

    private async Task ProcessCryptoSpikeTask(Services.Models.Task task, Dictionary<string, (List<string> events, decimal score)> cryptoEvents, Dictionary<string, CryptoMetrics> cryptoMetrics, CancellationToken stoppingToken)
    {
        try
        {
            var relevantEvents = new Dictionary<string, (List<string> events, decimal score)>();
            var relevantMetrics = new Dictionary<string, CryptoMetrics>();

            // Check which symbols from the task have events or metrics
            foreach (var symbol in task.Symbols)
            {
                if (cryptoEvents.ContainsKey(symbol))
                {
                    relevantEvents[symbol] = cryptoEvents[symbol];
                }

                if (cryptoMetrics.ContainsKey(symbol))
                {
                    relevantMetrics[symbol] = cryptoMetrics[symbol];
                }
            }

            // Only include symbols that have events in the email
            var symbolsWithEvents = relevantEvents.Keys.ToList();
            
            if (!symbolsWithEvents.Any())
            {
                logger.LogInformation("No symbols with events for user {email}, skipping email", task.Email);
                return;
            }

            // Filter metrics to only include symbols that have events
            var filteredMetrics = relevantMetrics
                .Where(x => symbolsWithEvents.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            // Create email content
            var emailBody = "Crypto Metrics Report\n\n";

            // Section 1: Primary Symbols (symbols marked as primary)
            var primarySymbols = filteredMetrics
                .Where(x => task.PrimarySymbols.Contains(x.Key))
                .OrderByDescending(x => relevantEvents.ContainsKey(x.Key) ? relevantEvents[x.Key].score : 0)
                .ToList();

            if (primarySymbols.Any())
            {
                emailBody += "⭐ PRIMARY SYMBOLS ⭐\n";
                emailBody += "===================\n\n";

                foreach (var kvp in primarySymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events Detected:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Daily Change: {metrics.DailyPriceChange:F8}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 2: The Dropper of the day (symbols that hit new absolute minimums today)
            var dailyDroppersForTask = _dailyDroppers
                .Where(x => task.Symbols.Contains(x.Key))
                .OrderByDescending(x => x.Value.PreviousMin - x.Value.CurrentMin) // Order by biggest drop (previous - current)
                .ToList();

            if (dailyDroppersForTask.Any())
            {
                emailBody += "📉 THE DROPPER OF THE DAY 📉\n";
                emailBody += "============================\n\n";

                foreach (var kvp in dailyDroppersForTask)
                {
                    var symbol = kvp.Key;
                    var dropperInfo = kvp.Value;
                    var dropAmount = dropperInfo.PreviousMin - dropperInfo.CurrentMin;
                    var dropPercentage = (dropAmount / dropperInfo.PreviousMin) * 100;
                    var currentPercentage = (cryptoMetrics[symbol].CurrentPrice / dropperInfo.CurrentMin) * 100;

                    emailBody += $"Symbol: {symbol}\n";
                    emailBody += $"Current Price: {cryptoMetrics[symbol].CurrentPrice:F8}\n";
                    emailBody += $"Current Percentage: {currentPercentage:F2}%\n";
                    emailBody += $"Average Daily Price: {cryptoMetrics[symbol].AveragePrice:F8}\n";
                    emailBody += $"Previous Absolute Min: {dropperInfo.PreviousMin:F8}\n";
                    emailBody += $"New Absolute Min: {dropperInfo.CurrentMin:F8}\n";
                    emailBody += $"Drop Amount: {dropAmount:F8}\n";
                    emailBody += $"Drop Percentage: {dropPercentage:F2}%\n";
                    emailBody += $"Time of Drop: {dropperInfo.TimeStamp:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 3: Most Volatile Symbols (top 10 by volatility count)
            var mostVolatileSymbols = relevantMetrics
                .Where(x => x.Value.DailyVolatilityCount > 0)
                .OrderByDescending(x => x.Value.DailyVolatilityCount)
                .Take(10)
                .ToList();

            if (mostVolatileSymbols.Any())
            {
                emailBody += "📈 MOST VOLATILE SYMBOLS (Top 10) 📈\n";
                emailBody += "=====================================\n\n";

                foreach (var kvp in mostVolatileSymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Daily Change: {metrics.DailyPriceChange:F8}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 3: Biggest Price Change (top 10 by daily price change)
            var biggestPriceChangeSymbols = relevantMetrics
                .Where(x => x.Value.DailyPriceChange > 0)
                .OrderByDescending(x => x.Value.DailyPriceChange)
                .Take(10)
                .ToList();

            if (biggestPriceChangeSymbols.Any())
            {
                emailBody += "💰 BIGGEST PRICE CHANGE (Top 10) 💰\n";
                emailBody += "==================================\n\n";

                foreach (var kvp in biggestPriceChangeSymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    emailBody += $"Daily Price Change: {metrics.DailyPriceChange:F8}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 4: Rest of Symbols (sorted by score)
            var restSymbols = filteredMetrics
                .Where(x => !task.PrimarySymbols.Contains(x.Key) && 
                           !mostVolatileSymbols.Any(v => v.Key == x.Key) && 
                           !biggestPriceChangeSymbols.Any(b => b.Key == x.Key) &&
                           !dailyDroppersForTask.Any(d => d.Key == x.Key))
                .OrderByDescending(x => relevantEvents.ContainsKey(x.Key) ? relevantEvents[x.Key].score : 0)
                .ToList();

            if (restSymbols.Any())
            {
                emailBody += "📊 REMAINING SYMBOLS (Sorted by Score) 📊\n";
                emailBody += "==========================================\n\n";

                foreach (var kvp in restSymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Daily Change: {metrics.DailyPriceChange:F8}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            emailBody += "This report was generated automatically by Libri Genie Crypto Spikes detection system.";

            // Send email
            var subject = "Crypto Spike Alerts & Metrics";
            await mailService.Send(task.Email, subject, emailBody, stoppingToken);

            logger.LogInformation("Sent crypto report to {email} for {count} symbols with events (total symbols: {totalSymbols})",
                task.Email, symbolsWithEvents.Count, task.Symbols.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing crypto spike task for {email}: {message}", task.Email, ex.Message);
        }
    }
}
