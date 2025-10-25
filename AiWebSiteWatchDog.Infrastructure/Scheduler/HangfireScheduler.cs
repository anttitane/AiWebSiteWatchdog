using Hangfire;
using System;
using System.Threading.Tasks;

namespace AiWebSiteWatchDog.Infrastructure.Scheduler
{
    public class HangfireScheduler()
    {
        public void ScheduleJob(string recurringJobId, string cronExpression, Func<Task> job)
        {
            // Use local time zone so cron expressions match server local time
            var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Local };
            RecurringJob.AddOrUpdate(recurringJobId, () => job(), cronExpression, options);
        }
    }
}
