using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Application.Services
{
    public class NotificationService : INotificationService
    {
        public Task SendNotificationAsync(Notification notification)
        {
            // TODO: Implement email sending logic
            return Task.CompletedTask;
        }
    }
}
