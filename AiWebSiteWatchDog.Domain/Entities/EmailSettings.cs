using System.ComponentModel.DataAnnotations;
namespace AiWebSiteWatchDog.Domain.Entities
{
    public class EmailSettings(string senderEmail, string senderName, string gmailClientSecretJson)
    {
        [Key]
        public string SenderEmail { get; set; } = senderEmail;
        public string SenderName { get; set; } = senderName;
        public string GmailClientSecretJson { get; set; } = gmailClientSecretJson;
    }
}
