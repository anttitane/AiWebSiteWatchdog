namespace AiWebSiteWatchDog.Domain.DTOs
{
    public record WatchTaskDto(
    int Id,
    string Title,
        string Url,
        string TaskPrompt,
        string Schedule,
        System.DateTime LastChecked,
        string? LastResult);
}
