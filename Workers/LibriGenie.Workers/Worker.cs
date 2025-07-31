using LibriGenie.Workers.Services;
using LibriGenie.Workers.Services.Brevo;
using LibriGenie.Workers.Services.ContentGenerate;
using System.Text.Json;

namespace LibriGenie.Workers;

public class Worker(ILibriGenieClient libriGenieClient, IWordpressPublisher wordpressPublisher, IContentGenerator contentGenerator, IMailService mailService, ICryptoManager cryptoManager, ILogger<Worker> logger) : BackgroundService
{
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
                                await mailService.SendTextFromNoReply(task.Email, content.Title, content.Content, stoppingToken);

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

            if (!relevantEvents.Any() && !relevantMetrics.Any())
            {
                logger.LogInformation("No crypto data for user {email} symbols", task.Email);
                return;
            }

            // Create email content
            var emailBody = "Crypto Metrics Report\n\n";

            // Add events section if there are any
            if (relevantEvents.Any() && relevantMetrics.Any())
            {
                emailBody += "🚨 SPIKE ALERTS DETECTED 🚨\n\n";

                foreach (var kvp in relevantEvents.OrderByDescending(x => x.Value.score))
                {
                    var symbol = kvp.Key;
                    var events = kvp.Value.events;

                    emailBody += $"Symbol: {symbol}\n";
                    emailBody += "Events Detected:\n";
                    foreach (var evt in events)
                    {
                        emailBody += $"  • {evt}\n";
                    }

                    var metrics = relevantMetrics[symbol];
                    emailBody += $"Metrics:\n";
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            emailBody += "This report was generated automatically by Libri Genie Crypto Spikes detection system.";

            // Send email
            var subject = relevantEvents.Any() ? "Crypto Spike Alerts & Metrics" : "Crypto Metrics Report";
            await mailService.SendTextFromNoReply(task.Email, subject, emailBody, stoppingToken);

            logger.LogInformation("Sent crypto report to {email} for {count} symbols (events: {eventCount}, metrics: {metricCount})",
                task.Email, task.Symbols.Count, relevantEvents.Count, relevantMetrics.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing crypto spike task for {email}: {message}", task.Email, ex.Message);
        }
    }
}
