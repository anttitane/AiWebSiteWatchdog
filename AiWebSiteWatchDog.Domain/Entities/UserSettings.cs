namespace AiWebSiteWatchDog.Domain.Entities
{
    public class UserSettings(string emailRecipient, string watchUrl, string interestSentence, string schedule, string emailSettingsSenderEmail)
    {
        public string EmailRecipient { get; set; } = emailRecipient;
        public string WatchUrl { get; set; } = watchUrl;
        public string InterestSentence { get; set; } = interestSentence;
        public string Schedule { get; set; } = schedule; // e.g., cron expression
        public string EmailSettingsSenderEmail { get; set; } = emailSettingsSenderEmail;
        public EmailSettings? EmailSettings { get; set; }
    }
}
