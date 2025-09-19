using System;

namespace AiWebSiteWatchDog.Domain.Entities
{
    public class WatchTask
    {
        public int Id { get; set; } // EF Core primary key
        public string Url { get; set; } = string.Empty;
        public string InterestSentence { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public string? LastResult { get; set; }
    }
}
