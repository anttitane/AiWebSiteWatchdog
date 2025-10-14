using System.Collections.Generic;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class WatchTaskRepository(AppDbContext _dbContext)
    {
        public async Task<List<WatchTask>> GetAllAsync()
        {
            return await _dbContext.WatchTasks.AsNoTracking().ToListAsync();
        }

        public async Task<WatchTask?> GetByIdAsync(int id)
        {
            return await _dbContext.WatchTasks.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task AddAsync(WatchTask task)
        {
            // Ensure FK set (single user assumption)
            if (string.IsNullOrEmpty(task.UserSettingsId))
            {
                var userSettings = await _dbContext.UserSettings.FirstOrDefaultAsync();
                if (userSettings == null)
                {
                    // Create a default user settings row if none exists
                    userSettings = new UserSettings(userEmail: "user@example.com", senderEmail: "user@example.com", senderName: "User");
                    _dbContext.UserSettings.Add(userSettings);
                    await _dbContext.SaveChangesAsync();
                }
                task.UserSettingsId = userSettings.UserEmail;
            }
            await _dbContext.WatchTasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();
            Log.Information("WatchTask saved to database with UserSettingsId {UserSettingsId}.", task.UserSettingsId);
        }

        public async Task<bool> UpdateAsync(int id, WatchTask updated)
        {
            var existing = await _dbContext.WatchTasks.FindAsync(id);
            if (existing == null) return false;
            _dbContext.Entry(existing).CurrentValues.SetValues(updated);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _dbContext.WatchTasks.FindAsync(id);
            if (existing == null) return false;
            _dbContext.WatchTasks.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
