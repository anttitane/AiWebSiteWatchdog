using System.Collections.Generic;
namespace AiWebSiteWatchDog.Domain.Entities
{
    public class UserSettings(string userEmail, string senderEmail, string senderName)
    {
        public string UserEmail { get; set; } = userEmail;
        public string SenderEmail { get; set; } = senderEmail;
        public string SenderName { get; set; } = senderName;
        public ICollection<WatchTask> WatchTasks { get; set; } = new List<WatchTask>();
    }
}
