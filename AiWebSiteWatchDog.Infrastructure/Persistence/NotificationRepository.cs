using System.Collections.Generic;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class NotificationRepository
    {
        private readonly AppDbContext _dbContext;

        public NotificationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Notification>> GetAllAsync()
        {
            return await _dbContext.Notifications.ToListAsync();
        }

        public async Task AddAsync(Notification notification)
        {
            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();
            Log.Information("Notification saved to database.");
        }
    }
}
