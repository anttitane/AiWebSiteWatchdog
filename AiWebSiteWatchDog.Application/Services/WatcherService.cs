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
            var currentSettings = await _settingsService.GetSettingsAsync();
            var result = await _geminiApiClient.CheckInterestAsync(settings.WatchUrl, settings.InterestSentence, settings.GeminiApiKey);
            return new WatchTask(settings.EmailSettingsId, settings.WatchUrl, settings.InterestSentence, DateTime.UtcNow, result);
        }
    }
}
