using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(Notification notification, UserSettings settings, string recipientEmail);
    }
}
