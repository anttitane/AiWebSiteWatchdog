namespace AiWebSiteWatchDog.API.Configuration
{
    public class RateLimitingOptions
    {
        public GlobalOptions Global { get; set; } = new();
        public FixedWindowPolicyOptions StrictPerIp { get; set; } = new();
        public FixedWindowPolicyOptions RunTaskPerIp { get; set; } = new();
        public ConcurrencyPolicyOptions RunTaskConcurrencyPerIp { get; set; } = new();
        public RejectionOptions Rejection { get; set; } = new();

        public class GlobalOptions : FixedWindowPolicyOptions
        {
        }

        public class FixedWindowPolicyOptions
        {
            public int PermitLimit { get; set; } = 60;
            public int WindowSeconds { get; set; } = 60;
            public int QueueLimit { get; set; } = 0;
        }

        public class ConcurrencyPolicyOptions
        {
            public int PermitLimit { get; set; } = 2;
            public int QueueLimit { get; set; } = 4;
        }

        public class RejectionOptions
        {
            public int RetryAfterSeconds { get; set; } = 60;
            public string Message { get; set; } = "Too many requests. Please try again later.";
        }
    }
}
