using LibriGenie.Api.Data.Models;
using MongoDB.Driver;
using NCrontab;

namespace LibriGenie.Api.Services;

public class TaskService(IMongoDatabase database) : ITaskService
{
    public async Task<IList<Models.Task>> GetTasksForRun(int page, int pageSize, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var timeOfDay = utcNow.TimeOfDay;
        var timePlusFive = timeOfDay.Add(TimeSpan.FromMinutes(5));

        var users = await database
            .GetCollection<User>("users")
            .Find(x => x.Settings != null && x.Settings.Any())
            .Skip(page * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var tasks = new List<Models.Task>();

        foreach (var user in users)
        {
            foreach (var setting in user.Settings)
            {
                if (!setting.Enable) continue;

                bool shouldRun = false;

                if (setting.EventBase)
                {
                    // For event base tasks, ignore Time and LastRun constraints
                    shouldRun = true;
                }
                else if (setting.TypeTrigger == 0)
                {
                    // Time-based scheduling (TypeTrigger = 0)
                    shouldRun = (setting.LastRun == null || setting.LastRun.Value.Date != utcNow.Date)
                              && (setting.Time <= timePlusFive);
                }
                else if (setting.TypeTrigger == 1 && !string.IsNullOrEmpty(setting.Cron))
                {
                    // Cron-based scheduling (TypeTrigger = 1)
                    try
                    {
                        var schedule = CrontabSchedule.Parse(setting.Cron);
                        var nextOccurrence = schedule.GetNextOccurrence(setting.LastRun ?? utcNow.AddDays(-1));
                        
                        // Check if the next occurrence is within the next 5 minutes
                        shouldRun = nextOccurrence <= utcNow.AddMinutes(5);
                    }
                    catch (CrontabException ex)
                    {
                        // Log invalid cron expression but don't crash
                        Console.WriteLine($"Invalid cron expression for setting {setting.Id}: {ex.Message}");
                        shouldRun = false;
                    }
                }

                if (shouldRun)
                {
                    tasks.Add(new Models.Task()
                    {
                        Id = $"{user.Id}_{setting.Id}", // Combine user ID and setting ID
                        Email = user.Email,
                        Category = setting.Category,
                        EnableWordpress = setting.EnableWordpress,
                        UsernameWordpress = setting.UsernameWordpress,
                        PasswordWordpress = setting.PasswordWordpress,
                        UrlWordpress = setting.UrlWordpress,
                        Time = setting.Time,
                        LastRun = setting.LastRun,
                        Symbols = setting.Symbols ?? new List<string>(),
                        PrimarySymbols = setting.PrimarySymbols ?? new List<string>(),
                        CoinbaseName = setting.CoinbaseName,
                        CoinbasePrivateKey = setting.CoinbasePrivateKey,
                        EventBase = setting.EventBase,
                        TypeTrigger = setting.TypeTrigger,
                        Cron = setting.Cron,
                    });
                }
            }
        }

        return tasks;
    }

    public async Task<IList<Models.Task>> GetAllActiveTasks(CancellationToken cancellationToken)
    {
        var users = await database
            .GetCollection<User>("users")
            .Find(x => x.Settings != null && x.Settings.Any())
            .ToListAsync(cancellationToken);

        var tasks = new List<Models.Task>();

        foreach (var user in users)
        {
            foreach (var setting in user.Settings)
            {
                if (setting.Enable)
                {
                    tasks.Add(new Models.Task()
                    {
                        Id = $"{user.Id}_{setting.Id}", // Combine user ID and setting ID
                        Email = user.Email,
                        Category = setting.Category,
                        EnableWordpress = setting.EnableWordpress,
                        UsernameWordpress = setting.UsernameWordpress,
                        PasswordWordpress = setting.PasswordWordpress,
                        UrlWordpress = setting.UrlWordpress,
                        Time = setting.Time,
                        LastRun = setting.LastRun,
                        Symbols = setting.Symbols ?? new List<string>(),
                        PrimarySymbols = setting.PrimarySymbols ?? new List<string>(),
                        CoinbaseName = setting.CoinbaseName,
                        CoinbasePrivateKey = setting.CoinbasePrivateKey,
                        EventBase = setting.EventBase,
                        TypeTrigger = setting.TypeTrigger,
                        Cron = setting.Cron,
                    });
                }
            }
        }

        return tasks;
    }

    public async System.Threading.Tasks.Task SetLastRun(string id, CancellationToken cancellationToken)
    {
        // Parse the combined ID to get user ID and setting ID
        var parts = id.Split('_');
        if (parts.Length != 2) return;
        
        var userId = parts[0];
        var settingId = parts[1];
        
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(x => x.Id, userId),
            Builders<User>.Filter.ElemMatch(x => x.Settings, s => s.Id == settingId)
        );
        
        var update = Builders<User>.Update.Set("settings.$.lastRun", DateTime.UtcNow);
        
        await database.GetCollection<User>("users").UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
