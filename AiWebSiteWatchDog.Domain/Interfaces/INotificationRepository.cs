using System.Collections.Generic;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetAllAsync();
        Task<Notification?> GetByIdAsync(int id);
        Task AddAsync(Notification notification);
        Task<bool> DeleteAsync(int id);
        Task<int> DeleteOlderThanAsync(System.DateTime cutoffDate);
    }
}
