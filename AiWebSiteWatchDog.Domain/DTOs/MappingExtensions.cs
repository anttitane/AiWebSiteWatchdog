using System.Linq;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.DTOs
{
    public static class MappingExtensions
    {
        public static WatchTaskDto ToDto(this WatchTask task) =>
            new(task.Id, task.Title, task.Url, task.TaskPrompt, task.Schedule, task.LastChecked, task.LastResult, task.Enabled);

        public static WatchTaskSummaryDto ToSummaryDto(this WatchTask task) =>
            new(task.Id, task.Title, task.Url, task.Schedule, task.Enabled);

        private static string? MaskToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var keep = 6; // show last 6 characters for identification
            if (token.Length <= keep) return token; // very short token; show as-is
            return new string('*', token.Length - keep) + token[^keep..];
        }

        public static UserSettingsDto ToDto(this UserSettings settings) =>
            new(settings.UserEmail,
                settings.SenderEmail,
                settings.SenderName,
                settings.GeminiApiUrl,
                settings.NotificationChannel.ToString(),
                MaskToken(settings.TelegramBotToken),
                settings.TelegramChatId,
                settings.NotificationRetentionDays,
                settings.WatchTasks.Select(t => t.ToSummaryDto()).ToList());

        public static NotificationDto ToDto(this Notification notification) =>
            new(notification.Id, notification.Subject, notification.Message, notification.SentAt);
    }
}
