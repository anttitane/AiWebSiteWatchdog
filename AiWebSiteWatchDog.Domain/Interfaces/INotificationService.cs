using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.DTOs;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> SendNotificationAsync(CreateNotificationRequest request);
    }
}
