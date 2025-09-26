namespace AiWebSiteWatchDog.Domain.Entities
{
    public class UserSettings(string emailRecipient, string emailSettingsSenderEmail)
    {
        public string EmailRecipient { get; set; } = emailRecipient;
        public string EmailSettingsSenderEmail { get; set; } = emailSettingsSenderEmail;
        public EmailSettings? EmailSettings { get; set; }
    }
}
