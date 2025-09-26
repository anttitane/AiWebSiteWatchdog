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

        // Update: Now expects WatchTask as input, not UserSettings
        public async Task<WatchTask> CheckWebsiteAsync(WatchTask task)
        {
            var result = await _geminiApiClient.CheckInterestAsync(task.Url, task.TaskPrompt);
            task.LastChecked = DateTime.UtcNow;
            task.LastResult = result;
            return task;
        }
    }
}
