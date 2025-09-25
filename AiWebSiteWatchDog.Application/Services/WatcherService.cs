using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Application.Services
{
    public class WatcherService(IGeminiApiClient geminiApiClient, ISettingsService settingsService) : IWatcherService
    {
        private readonly IGeminiApiClient _geminiApiClient = geminiApiClient;
        private readonly ISettingsService _settingsService = settingsService;

        public async Task<WatchTask> CheckWebsiteAsync(UserSettings settings)
        {
            var result = await _geminiApiClient.CheckInterestAsync(settings.WatchUrl, settings.InterestSentence);
            
            return new WatchTask
            {
                Url = settings.WatchUrl,
                InterestSentence = settings.InterestSentence,
                LastChecked = DateTime.UtcNow,
                LastResult = result
            };
        }
    }
}
