using LibriGenie.Workers.Services;
using LibriGenie.Workers.Services.Brevo;
using LibriGenie.Workers.Services.ContentGenerate;
using System.Text.Json;

namespace LibriGenie.Workers;

public class Worker(ILibriGenieClient libriGenieClient, IWordpressPublisher wordpressPublisher, IContentGenerator contentGenerator, IMailService mailService, ILogger<Worker> logger) : BackgroundService
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
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            try
            {
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
                            var content = await contentGenerator.Generate(task.Category, stoppingToken);
                            await mailService.SendTextFromNoReply(task.Email, content.Title, content.Content, stoppingToken);

                            if (task.EnableWordpress)
                            {
                                await wordpressPublisher.Publish(task, content.Title, content.Content, stoppingToken);
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

            await Task.Delay(10000, stoppingToken);
        }
    }
}
