namespace AiWebSiteWatchDog.Domain.DTOs
{
    // Shape for creating new watch tasks via POST; Id is not accepted from clients
    public record CreateWatchTaskRequest(
        string Title,
        string Url,
        string TaskPrompt,
        string? Schedule
    );
}
