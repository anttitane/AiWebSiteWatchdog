using System.Collections.Generic;
using AiWebSiteWatchDog.Domain.Constants;
namespace AiWebSiteWatchDog.Domain.Entities
{
    public class UserSettings(string userEmail, string senderEmail, string senderName)
    {
        public string UserEmail { get; set; } = userEmail;
        public string SenderEmail { get; set; } = senderEmail;
        public string SenderName { get; set; } = senderName;
        public string GeminiApiUrl { get; set; } = GeminiDefaults.ApiUrl;
        public NotificationChannel NotificationChannel { get; set; } = NotificationChannel.Email;
        public string? TelegramBotToken { get; set; }
        public string? TelegramChatId { get; set; }
        public int NotificationRetentionDays { get; set; } = 30;
        public ICollection<WatchTask> WatchTasks { get; set; } = new List<WatchTask>();
    }
}
