using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using AiWebSiteWatchDog.Application.Parsing;
using Hangfire;
using Serilog;

namespace AiWebSiteWatchDog.API.Jobs
{
    public class WatchTaskJobRunner(
        Infrastructure.Persistence.WatchTaskRepository repo,
        IWatcherService watcherService,
        INotificationService notificationService)
    {
        private readonly Infrastructure.Persistence.WatchTaskRepository _repo = repo;
        private readonly IWatcherService _watcherService = watcherService;
        private readonly INotificationService _notificationService = notificationService;

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task ExecuteAsync(int id)
        {
            var task = await _repo.GetByIdAsync(id);
            if (task is null) return;
            // Guard against accidental rapid re-entry (e.g., duplicate enqueues)
            if (task.LastChecked != default && task.LastChecked > DateTime.UtcNow.AddSeconds(-30))
            {
                Log.Warning("Skipping task {TaskId} because it was executed recently at {LastChecked}", id, task.LastChecked);
                return;
            }

            var updated = await _watcherService.CheckWebsiteAsync(task);
            await _repo.UpdateAsync(id, updated);

            var subject = $"AiWebSiteWatchDog results for task - {updated.Title}";
            var message = GeminiResponseParser.ExtractText(updated.LastResult) ?? "(no content)";
            await _notificationService.SendNotificationAsync(new Domain.DTOs.CreateNotificationRequest(subject, message));
        }
    }
}