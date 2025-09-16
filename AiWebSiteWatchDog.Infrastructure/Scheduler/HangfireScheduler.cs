using Hangfire;
using System;
using System.Threading.Tasks;

namespace AiWebSiteWatchDog.Infrastructure.Scheduler
{
    public class HangfireScheduler
    {
        public void ScheduleJob(string cronExpression, Func<Task> job)
        {
            RecurringJob.AddOrUpdate(() => job(), cronExpression);
        }
    }
}
