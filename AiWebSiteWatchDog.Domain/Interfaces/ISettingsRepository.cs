using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface ISettingsRepository
    {
        Task<UserSettings> LoadAsync();
        Task SaveAsync(UserSettings settings);
    }
}
