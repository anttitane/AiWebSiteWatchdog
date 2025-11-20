using System.Collections.Generic;

namespace AiWebSiteWatchDog.Domain.DTOs
{
    public record UserSettingsDto(
        string UserEmail,
        string SenderEmail,
        string SenderName,
        string GeminiApiUrl,
        int NotificationChannel,
        string? TelegramBotToken,
        string? TelegramChatId,
        IReadOnlyCollection<WatchTaskSummaryDto> WatchTasks);

    public record WatchTaskSummaryDto(
        int Id,
        string Title,
        string Url,
        string Schedule,
        bool Enabled);
}
