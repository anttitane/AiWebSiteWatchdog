using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface IWatcherService
    {
        Task<WatchTask> CheckWebsiteAsync(WatchTask task);
    }
}
