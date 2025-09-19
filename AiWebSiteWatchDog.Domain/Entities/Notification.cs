using System;

namespace AiWebSiteWatchDog.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; } // EF Core primary key
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
