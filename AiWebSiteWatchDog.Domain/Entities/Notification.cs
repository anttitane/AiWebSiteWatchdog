using System;

namespace AiWebSiteWatchDog.Domain.Entities
{
    public class Notification(int id, string subject, string message, DateTime sentAt)
    {
        public int Id { get; set; } = id;
        public string Subject { get; set; } = subject;
        public string Message { get; set; } = message;
        public DateTime SentAt { get; set; } = sentAt;
    }
}
