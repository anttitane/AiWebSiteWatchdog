using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Application.Services
{
    public class SettingsService(ISettingsRepository repository) : ISettingsService
    {
        private readonly ISettingsRepository _repository = repository;

        public async Task<UserSettings> GetSettingsAsync()
        {
            return await _repository.LoadAsync();
        }

        public async Task SaveSettingsAsync(UserSettings settings)
        {
            await _repository.SaveAsync(settings);
        }
    }
}
