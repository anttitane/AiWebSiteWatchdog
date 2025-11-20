using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface ITelegramSender
    {
        Task SendAsync(Notification notification, UserSettings settings, string? chatIdOverride = null);
    }
}
