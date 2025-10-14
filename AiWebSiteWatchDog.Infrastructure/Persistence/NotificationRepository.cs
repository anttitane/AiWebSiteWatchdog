using System.Collections.Generic;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
    }
}
