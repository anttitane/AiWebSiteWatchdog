namespace AiWebSiteWatchDog.Domain.DTOs
{
    // All fields optional to allow partial updates via PUT; API layer enforces Title requirement.
    public record UpdateWatchTaskRequest(
        string? Title,
        string? Url,
        string? TaskPrompt,
        string? Schedule,
        System.DateTime? LastChecked,
        string? LastResult,
        bool? Enabled);
}
