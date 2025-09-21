namespace AiWebSiteWatchDog.Domain.Entities
{
    public class EmailSettings(int id, string smtpServer, int smtpPort, string senderEmail, string senderName, string appPassword, bool enableSsl)
    {
        public int Id { get; set; } = id;
        public string SmtpServer { get; set; } = smtpServer;
        public int SmtpPort { get; set; } = smtpPort;
        public string SenderEmail { get; set; } = senderEmail;
        public string SenderName { get; set; } = senderName;
        public string AppPassword { get; set; } = appPassword;
        public bool EnableSsl { get; set; } = enableSsl;
    }
}
