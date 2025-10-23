using System;

namespace AiWebSiteWatchDog.Domain.DTOs
{
    public record CreateNotificationRequest(string Subject, string Message);
    public record NotificationDto(int Id, string Subject, string Message, DateTime SentAt);
}
