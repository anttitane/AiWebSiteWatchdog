using System.Linq;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.DTOs
{
    public static class MappingExtensions
    {
        public static WatchTaskDto ToDto(this WatchTask task) =>
            new(task.Id, task.Url, task.TaskPrompt, task.Schedule, task.LastChecked, task.LastResult);

        public static WatchTaskSummaryDto ToSummaryDto(this WatchTask task) =>
            new(task.Id, task.Url, task.Schedule);

        public static UserSettingsDto ToDto(this UserSettings settings) =>
            new(settings.UserEmail, settings.SenderEmail, settings.SenderName, settings.WatchTasks.Select(t => t.ToSummaryDto()).ToList());

        public static NotificationDto ToDto(this Notification notification) =>
            new(notification.Id, notification.Subject, notification.Message, notification.SentAt);
    }
}
