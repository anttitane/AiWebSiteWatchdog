using System.Collections.Generic;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class NotificationRepository(AppDbContext _dbContext) : INotificationRepository
    {
        public async Task<List<Notification>> GetAllAsync()
        {
            return await _dbContext.Notifications.ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _dbContext.Notifications.FindAsync(id);
        }

        public async Task AddAsync(Notification notification)
        {
            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();
            Log.Information("Notification saved to database.");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _dbContext.Notifications.FindAsync(id);
            if (existing == null) return false;
            _dbContext.Notifications.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<(List<int> deletedIds, List<int> notFoundIds)> DeleteManyAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return (new List<int>(), new List<int>());
            var items = await _dbContext.Notifications.Where(n => idList.Contains(n.Id)).ToListAsync();
            var foundIds = items.Select(n => n.Id).ToList();
            var notFound = idList.Except(foundIds).ToList();
            if (items.Count > 0)
            {
                _dbContext.Notifications.RemoveRange(items);
                await _dbContext.SaveChangesAsync();
            }
            return (foundIds, notFound);
        }

        public async Task<int> DeleteAllAsync()
        {
            var items = await _dbContext.Notifications.ToListAsync();
            if (items.Count == 0) return 0;
            _dbContext.Notifications.RemoveRange(items);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<int> DeleteOlderThanAsync(System.DateTime cutoffDate)
        {
            var items = await _dbContext.Notifications.Where(n => n.SentAt < cutoffDate).ToListAsync();
            if (items.Count == 0) return 0;
            _dbContext.Notifications.RemoveRange(items);
            return await _dbContext.SaveChangesAsync();
        }
    }
}
