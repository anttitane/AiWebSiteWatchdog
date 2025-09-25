using System.ComponentModel.DataAnnotations;
namespace AiWebSiteWatchDog.Domain.Entities
{
    public class EmailSettings(string senderEmail, string senderName)
    {
        [Key]
        public string SenderEmail { get; set; } = senderEmail;
        public string SenderName { get; set; } = senderName;
    }
}
