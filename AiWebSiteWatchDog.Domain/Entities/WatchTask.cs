using System;

namespace AiWebSiteWatchDog.Domain.Entities
{
    public class WatchTask(int id, string url, string interestSentence, DateTime lastChecked, string? lastResult)
    {
        public int Id { get; set; } = id;
        public string Url { get; set; } = url;
        public string InterestSentence { get; set; } = interestSentence;
        public DateTime LastChecked { get; set; } = lastChecked;
        public string? LastResult { get; set; } = lastResult;
    }
}
