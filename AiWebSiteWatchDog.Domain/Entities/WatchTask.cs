using System;

namespace AiWebSiteWatchDog.Domain.Entities
{
    public class WatchTask
    {
    public int Id { get; set; }
    public required string Url { get; set; }
    public required string InterestSentence { get; set; }
    public DateTime LastChecked { get; set; }
    public string? LastResult { get; set; }

        public WatchTask() { }

        public WatchTask(string url, string interestSentence, DateTime lastChecked, string? lastResult)
        {
            Url = url;
            InterestSentence = interestSentence;
            LastChecked = lastChecked;
            LastResult = lastResult;
        }

        public WatchTask(int id, string url, string interestSentence, DateTime lastChecked, string? lastResult)
        {
            Id = id;
            Url = url;
            InterestSentence = interestSentence;
            LastChecked = lastChecked;
            LastResult = lastResult;
        }
    }
}
