using System;

namespace AiWebSiteWatchDog.Domain.Entities
{
    public class Notification(int id, string email, string subject, string message, DateTime sentAt)
    {
        public int Id { get; set; } = id;
        public string Email { get; set; } = email;
        public string Subject { get; set; } = subject;
        public string Message { get; set; } = message;
        public DateTime SentAt { get; set; } = sentAt;
    }
}
