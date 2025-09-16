using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Application.Services
{
    public class WatcherService : IWatcherService
    {
        public Task<WatchTask> CheckWebsiteAsync(UserSettings settings)
        {
            // TODO: Implement website check logic
            return Task.FromResult(new WatchTask
            {
                Url = settings.WatchUrl,
                InterestSentence = settings.InterestSentence,
                LastChecked = DateTime.UtcNow,
                LastResult = null
            });
        }
    }
}
