using Hangfire;
using System;
using System.Threading.Tasks;

namespace AiWebSiteWatchDog.Infrastructure.Scheduler
{
    public class HangfireScheduler
    {
        public void ScheduleJob(string recurringJobId, string cronExpression, Func<Task> job)
        {
            var options = new RecurringJobOptions();
            RecurringJob.AddOrUpdate(recurringJobId, () => job(), cronExpression, options);
        }
    }
}
