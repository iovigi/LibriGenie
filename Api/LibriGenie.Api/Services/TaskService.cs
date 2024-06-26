﻿using LibriGenie.Api.Data.Models;
using MongoDB.Driver;

namespace LibriGenie.Api.Services;

public class TaskService(IMongoDatabase database) : ITaskService
{
    public async Task<IList<Models.Task>> GetTasksForRun(int page, int pageSize, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var timeOfDay = utcNow.TimeOfDay;
        var timePlusFive = timeOfDay.Add(TimeSpan.FromMinutes(5));

        return await database
            .GetCollection<User>("users")
            .Find(x => x.Settings != null
                && x.Settings.Enable
                && (x.Settings.LastRun == null || x.Settings.LastRun.Value.Date != utcNow.Date)
                && (x.Settings.Time <= timePlusFive))
            .Skip(page * pageSize)
            .Limit(pageSize)
            .Project(x => new Models.Task()
            {
                Id = x.Id,
                Email = x.Email,
                Category = x.Settings!.Category,
                EnableWordpress = x.Settings!.EnableWordpress,
                UsernameWordpress = x.Settings!.UsernameWordpress,
                PasswordWordpress = x.Settings!.PasswordWordpress,
                UrlWordpress = x.Settings!.UrlWordpress,
            })
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task SetLastRun(string id, CancellationToken cancellationToken)
    {
        await database.GetCollection<User>("users").UpdateOneAsync(x => x.Id == id, Builders<User>.Update.Set(z => z.Settings.LastRun, DateTime.UtcNow), cancellationToken: cancellationToken);
    }
}
