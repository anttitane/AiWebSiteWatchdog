using AiWebSiteWatchDog.Application.Services;
using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Application.Services
{
    public class WatcherService : IWatcherService
    {
        private readonly IGeminiApiClient _geminiApiClient;
        private readonly ISettingsService _settingsService;

        public WatcherService(IGeminiApiClient geminiApiClient, ISettingsService settingsService)
        {
            _geminiApiClient = geminiApiClient;
            _settingsService = settingsService;
        }

        public async Task<WatchTask> CheckWebsiteAsync(UserSettings settings)
        {
            var currentSettings = await _settingsService.GetSettingsAsync();
            var result = await _geminiApiClient.CheckInterestAsync(settings.WatchUrl, settings.InterestSentence, settings.GeminiApiKey);
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
