using System.Collections.Generic;
namespace AiWebSiteWatchDog.Domain.Entities
{
    public class UserSettings(string userEmail, string senderEmail, string senderName)
    {
        private const string DefaultGeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        public string UserEmail { get; set; } = userEmail;
        public string SenderEmail { get; set; } = senderEmail;
        public string SenderName { get; set; } = senderName;
        public string GeminiApiUrl { get; set; } = DefaultGeminiUrl;
        public ICollection<WatchTask> WatchTasks { get; set; } = new List<WatchTask>();
    }
}
