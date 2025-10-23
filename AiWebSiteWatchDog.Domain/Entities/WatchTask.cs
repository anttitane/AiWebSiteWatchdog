using System;
using System.Text.Json.Serialization;

namespace AiWebSiteWatchDog.Domain.Entities
{
    public class WatchTask
    {
        public int Id { get; set; }
        public string UserSettingsId { get; set; } = null!;
        [JsonIgnore] // Prevent JSON cycles when serializing tasks (UserSettings contains collection of WatchTasks)
        public UserSettings? UserSettings { get; set; }
        public string Title { get; set; } = string.Empty;
        public required string Url { get; set; }
        public required string TaskPrompt { get; set; }
        public string Schedule { get; set; } = string.Empty; // e.g., cron expression
        public DateTime LastChecked { get; set; }
        public string? LastResult { get; set; }
        public bool Enabled { get; set; } = true;

        public WatchTask() { }

        public WatchTask(string url, string taskPrompt, string schedule, DateTime lastChecked, string? lastResult)
        {
            Url = url;
            TaskPrompt = taskPrompt;
            Schedule = schedule;
            LastChecked = lastChecked;
            LastResult = lastResult;
        }

        public WatchTask(int id, string url, string taskPrompt, string schedule, DateTime lastChecked, string? lastResult)
        {
            Id = id;
            Url = url;
            TaskPrompt = taskPrompt;
            Schedule = schedule;
            LastChecked = lastChecked;
            LastResult = lastResult;
        }
    }
}
