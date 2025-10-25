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
        INotificationService notificationService)
    {
        private readonly Infrastructure.Persistence.WatchTaskRepository _repo = repo;
        private readonly IWatcherService _watcherService = watcherService;
        private readonly INotificationService _notificationService = notificationService;
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
                Log.Error(ex, "Scheduled task {TaskId} failed", id);

                // Persist basic failure info so UI and audits show the error
                try
                {
                    task.LastChecked = DateTime.UtcNow;
                    var errObj = new { error = ex.Message, details = ex.ToString() };
                    task.LastResult = JsonSerializer.Serialize(errObj);
                    await _repo.UpdateAsync(id, task);
                }
                catch (Exception persistEx)
                {
                    Log.Error(persistEx, "Failed to persist failure result for task {TaskId}", id);
                }

                // Notify user by email about the failed run. If notification sending fails, log and move on.
                try
                {
                    var subject = $"AiWebSiteWatchDog - task FAILED - {task.Title}";
                    var message = $"The scheduled task '{task.Title}' (id={task.Id}) failed at {DateTime.UtcNow:u}.\n\nError: {ex.Message}\n\nCheck application logs for details.";
                    await _notificationService.SendNotificationAsync(new Domain.DTOs.CreateNotificationRequest(subject, message));
                }
                catch (Exception notifyEx)
                {
                    Log.Error(notifyEx, "Failed to send failure notification for task {TaskId}", id);
                }
            }
        }
    }
}