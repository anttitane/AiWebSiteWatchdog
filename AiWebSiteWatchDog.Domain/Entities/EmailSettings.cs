namespace AiWebSiteWatchDog.Domain.Entities
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "AiWebSiteWatchdog";
        public string AppPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
