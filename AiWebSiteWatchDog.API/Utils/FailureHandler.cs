using System;
using System.Text.Json;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;

namespace AiWebSiteWatchDog.API.Utils
{
    public static class FailureHandler
    {
        /// <summary>
        /// Handle a task execution failure: log with correlation id, persist a sanitized LastResult,
        /// attempt to send a generic failure notification, and fall back to saving a notification record.
        /// Returns the generated correlation id for client responses.
        /// </summary>
        public static async Task<string> HandleFailureAsync(Exception ex, WatchTask task, int id,
            Infrastructure.Persistence.WatchTaskRepository repo,
            INotificationService notificationService,
            INotificationRepository notificationRepository,
            string executionContext)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            Log.Error(ex, "{ExecutionContext} for task {TaskId} failed (cid={CorrelationId})", executionContext, id, correlationId);

            try
            {
                task.LastChecked = DateTime.UtcNow;
                var errObj = new { correlationId, error = ex.Message };
                task.LastResult = JsonSerializer.Serialize(errObj);
                await repo.UpdateAsync(id, task);
            }
            catch (Exception persistEx)
            {
                Log.Error(persistEx, "Failed to persist failure result for task {TaskId} (cid={CorrelationId})", id, correlationId);
            }

            var subject = $"AiWebSiteWatchDog - task FAILED - {task.Title}";
            var message = $"The {executionContext} '{task.Title}' (id={task.Id}) failed at {DateTime.UtcNow:u}.\n\nReference: {correlationId}\n\nCheck application logs for details.";
            try
            {
                await notificationService.SendNotificationAsync(new Domain.DTOs.CreateNotificationRequest(subject, message));
            }
            catch (Exception notifyEx)
            {
                Log.Error(notifyEx, "Failed to send failure notification for task {TaskId} (cid={CorrelationId})", id, correlationId);
                try
                {
                    var fallback = new Domain.Entities.Notification(0, subject, message, DateTime.UtcNow);
                    await notificationRepository.AddAsync(fallback);
                }
                catch (Exception saveEx)
                {
                    Log.Error(saveEx, "Failed to save fallback notification for task {TaskId} (cid={CorrelationId})", id, correlationId);
                }
            }

            return correlationId;
        }
    }
}
