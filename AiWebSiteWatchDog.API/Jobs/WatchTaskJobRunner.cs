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
                // Create correlation id to surface to users without leaking internals
                var correlationId = Guid.NewGuid().ToString("N");
                Log.Error(ex, "Scheduled task {TaskId} failed (cid={CorrelationId})", id, correlationId);

                // Persist sanitized failure info so UI shows a useful reference (avoid storing full stack traces)
                try
                {
                    task.LastChecked = DateTime.UtcNow;
                    var errObj = new { correlationId, error = ex.Message };
                    task.LastResult = JsonSerializer.Serialize(errObj);
                    await _repo.UpdateAsync(id, task);
                }
                catch (Exception persistEx)
                {
                    Log.Error(persistEx, "Failed to persist failure result for task {TaskId} (cid={CorrelationId})", id, correlationId);
                }

                // Notify user by email with a generic message containing the correlation id. If sending fails, fall back to saving a notification record.
                var subject = $"AiWebSiteWatchDog - task FAILED - {task.Title}";
                var message = $"The scheduled task '{task.Title}' (id={task.Id}) failed at {DateTime.UtcNow:u}.\n\nReference: {correlationId}\n\nCheck application logs for details.";
                try
                {
                    await _notificationService.SendNotificationAsync(new Domain.DTOs.CreateNotificationRequest(subject, message));
                }
                catch (Exception notifyEx)
                {
                    Log.Error(notifyEx, "Failed to send failure notification for task {TaskId} (cid={CorrelationId})", id, correlationId);
                    try
                    {
                        var fallback = new Domain.Entities.Notification(0, subject, message, DateTime.UtcNow);
                        await _notificationRepository.AddAsync(fallback);
                    }
                    catch (Exception saveEx)
                    {
                        Log.Error(saveEx, "Failed to save fallback notification for task {TaskId} (cid={CorrelationId})", id, correlationId);
                    }
                }
            }
        }
    }
}