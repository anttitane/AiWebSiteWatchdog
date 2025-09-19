namespace AiWebSiteWatchDog.Domain.Entities
{
    public class UserSettings
    {
        public string Email { get; set; } = string.Empty;
        public string GeminiApiKey { get; set; } = string.Empty;
        public string WatchUrl { get; set; } = string.Empty;
        public string InterestSentence { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty; // e.g., cron expression

        public int EmailSettingsId { get; set; }
        public EmailSettings? EmailSettings { get; set; }
    }
}
