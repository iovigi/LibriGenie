using LibriGenie.Workers.Services;
using LibriGenie.Workers.Services.Brevo;
using LibriGenie.Workers.Services.ContentGenerate;

namespace LibriGenie.Workers;

public class Worker(ILibriGenieClient libriGenieClient, IWordpressPublisher wordpressPublisher, IContentGenerator contentGenerator, IMailService mailService, ILogger<Worker> logger) : BackgroundService
{
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
                    tasks = await libriGenieClient.GetTasksForRun(page, page_size, stoppingToken);

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

                            await libriGenieClient.SetLastRun(task.Id, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                        }
                    }
                }
                while (tasks.Count == page_size);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

            await Task.Delay(10000, stoppingToken);
        }
    }
}
