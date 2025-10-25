using System;
using System.Threading.Tasks;
using System.Text.Json;
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
        INotificationService notificationService,
        INotificationRepository notificationRepository)
    {
        private readonly Infrastructure.Persistence.WatchTaskRepository _repo = repo;
        private readonly IWatcherService _watcherService = watcherService;
        private readonly INotificationService _notificationService = notificationService;
        private readonly INotificationRepository _notificationRepository = notificationRepository;
        private const int MinimumExecutionIntervalSeconds = 30; // guard window to avoid rapid re-entry

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task ExecuteAsync(int id)
        {
            var task = await _repo.GetByIdAsync(id);
            if (task is null) return;

            // Guard against accidental rapid re-entry (e.g., duplicate enqueues)
            if (task.LastChecked != default && task.LastChecked > DateTime.UtcNow.AddSeconds(-MinimumExecutionIntervalSeconds))
            {
                Log.Warning("Skipping task {TaskId} because it was executed recently at {LastChecked}", id, task.LastChecked);
                return;
            }

            try
            {
                var updated = await _watcherService.CheckWebsiteAsync(task);
                await _repo.UpdateAsync(id, updated);

                var subject = $"AiWebSiteWatchDog results for task - {updated.Title}";
                var message = GeminiResponseParser.ExtractText(updated.LastResult) ?? "(no content)";
                await _notificationService.SendNotificationAsync(new Domain.DTOs.CreateNotificationRequest(subject, message));
            }
            catch (Exception ex)
            {
                // Delegate failure handling to shared helper to avoid duplication
                await AiWebSiteWatchDog.API.Utils.FailureHandler.HandleFailureAsync(ex, task, id, _repo, _notificationService, _notificationRepository, "scheduled task");
            }
        }
    }
}