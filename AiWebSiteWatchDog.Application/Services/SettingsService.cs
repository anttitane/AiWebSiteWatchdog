using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Application.Services
{
    public class SettingsService : ISettingsService
    {
        private UserSettings _settings = new UserSettings();

        public Task<UserSettings> GetSettingsAsync()
        {
            // TODO: Load from persistence
            return Task.FromResult(_settings);
        }

        public Task SaveSettingsAsync(UserSettings settings)
        {
            // TODO: Save to persistence
            _settings = settings;
            return Task.CompletedTask;
        }
    }
}
