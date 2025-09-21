namespace AiWebSiteWatchDog.Domain.Entities
{
    public class UserSettings(string email, string geminiApiKey, string watchUrl, string interestSentence, string schedule, int emailSettingsId, EmailSettings? emailSettings)
    {
        public string Email { get; set; } = email;
        public string GeminiApiKey { get; set; } = geminiApiKey;
        public string WatchUrl { get; set; } = watchUrl;
        public string InterestSentence { get; set; } = interestSentence;
        public string Schedule { get; set; } = schedule; // e.g., cron expression

        public int EmailSettingsId { get; set; } = emailSettingsId;
        public EmailSettings? EmailSettings { get; set; } = emailSettings;
    }
}
