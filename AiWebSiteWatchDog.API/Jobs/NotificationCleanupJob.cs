using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Interfaces;
using Hangfire;
using Serilog;

namespace AiWebSiteWatchDog.API.Jobs
{
    public class NotificationCleanupJob(INotificationRepository notificationRepo, ISettingsService settingsService)
    {
        private readonly INotificationRepository _notificationRepo = notificationRepo;
        private readonly ISettingsService _settingsService = settingsService;

        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        public async Task ExecuteAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                if (settings == null)
                {
                    Log.Warning("Skipping notification cleanup: User settings not found.");
                    return;
                }

                // Default to 30 days if somehow 0 or negative
                int retentionDays = settings.NotificationRetentionDays > 0 ? settings.NotificationRetentionDays : 30;

                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                Log.Information("Starting notification cleanup. Deleting notifications older than {Cutoff} (Retention: {Days} days)", cutoff, retentionDays);

                int deletedCount = await _notificationRepo.DeleteOlderThanAsync(cutoff);
                
                if (deletedCount > 0)
                {
                    Log.Information("Notification cleanup completed. Deleted {Count} old notifications.", deletedCount);
                }
                else
                {
                    Log.Information("Notification cleanup completed. No old notifications found.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to cleanup old notifications");
                throw; // Retry job
            }
        }
    }
}
