using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface ISettingsService
    {
        Task<UserSettings> GetSettingsAsync();
        Task SaveSettingsAsync(UserSettings settings);
    }
}
