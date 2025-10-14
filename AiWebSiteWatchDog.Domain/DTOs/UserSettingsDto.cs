using System.Collections.Generic;

namespace AiWebSiteWatchDog.Domain.DTOs
{
    public record UserSettingsDto(
        string UserEmail,
        string SenderEmail,
        string SenderName,
        IReadOnlyCollection<WatchTaskSummaryDto> WatchTasks);

    public record WatchTaskSummaryDto(
        int Id,
        string Url,
        string Schedule);
}
