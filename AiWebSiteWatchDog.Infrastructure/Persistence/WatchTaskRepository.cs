using System.Collections.Generic;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using AiWebSiteWatchDog.Domain.Constants;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class WatchTaskRepository(AppDbContext _dbContext, IMemoryCache cache)
    {
        private readonly IMemoryCache _cache = cache;
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
                var userSettings = await _dbContext.UserSettings
                    .OrderBy(u => u.UserEmail)
                    .FirstOrDefaultAsync();
                if (userSettings == null)
                {
                    // Create a default user settings row if none exists
                    userSettings = new UserSettings(userEmail: "user@example.com", senderEmail: "user@example.com", senderName: "User");
                    _dbContext.UserSettings.Add(userSettings);
                    await _dbContext.SaveChangesAsync();
                }
                task.UserSettingsId = userSettings.UserEmail;
            }

            // Ensure Title has a sensible default
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                task.Title = string.IsNullOrWhiteSpace(task.TaskPrompt) ? task.Url : task.TaskPrompt;
            }
            await _dbContext.WatchTasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();
            Log.Information("WatchTask saved to database with UserSettingsId {UserSettingsId}.", task.UserSettingsId);
            // Invalidate settings cache so /settings returns fresh watchTasks
            _cache.Remove(SettingsCacheKeys.UserSettingsSingleton);
        }

        public async Task<bool> UpdateAsync(int id, WatchTask updated)
        {
            var existing = await _dbContext.WatchTasks.FindAsync(id);
            if (existing == null) return false;

            // Update only allowed mutable fields; never modify PK or owner FK here
            if (!string.IsNullOrWhiteSpace(updated.Title))
                existing.Title = updated.Title.Trim();
            else if (string.IsNullOrWhiteSpace(existing.Title))
                existing.Title = string.IsNullOrWhiteSpace(existing.TaskPrompt) ? existing.Url : existing.TaskPrompt;

            if (!string.IsNullOrWhiteSpace(updated.Url))
                existing.Url = updated.Url;

            if (!string.IsNullOrWhiteSpace(updated.TaskPrompt))
                existing.TaskPrompt = updated.TaskPrompt;

            if (updated.Schedule != null)
                existing.Schedule = updated.Schedule;

            // Enabled pause/resume toggle
            existing.Enabled = updated.Enabled;

            // Optional: allow updating last check/result if provided
            if (updated.LastChecked != default)
                existing.LastChecked = updated.LastChecked;
            if (updated.LastResult != null)
                existing.LastResult = updated.LastResult;

            await _dbContext.SaveChangesAsync();
            // Invalidate settings cache so /settings reflects updates immediately
            _cache.Remove(SettingsCacheKeys.UserSettingsSingleton);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _dbContext.WatchTasks.FindAsync(id);
            if (existing == null) return false;
            _dbContext.WatchTasks.Remove(existing);
            await _dbContext.SaveChangesAsync();
            // Invalidate settings cache so deleted tasks disappear from /settings
            _cache.Remove(SettingsCacheKeys.UserSettingsSingleton);
            return true;
        }

        public async Task<(List<int> deletedIds, List<int> notFoundIds)> DeleteManyAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return (new List<int>(), new List<int>());
            var tasks = await _dbContext.WatchTasks.Where(w => idList.Contains(w.Id)).ToListAsync();
            var foundIds = tasks.Select(t => t.Id).ToList();
            var notFound = idList.Except(foundIds).ToList();
            if (tasks.Count > 0)
            {
                _dbContext.WatchTasks.RemoveRange(tasks);
                await _dbContext.SaveChangesAsync();

                // Invalidate settings cache after bulk delete
                _cache.Remove(SettingsCacheKeys.UserSettingsSingleton);
            }
            return (foundIds, notFound);
        }

        public async Task<int> DeleteAllAsync()
        {
            var tasks = await _dbContext.WatchTasks.ToListAsync();
            if (tasks.Count == 0) return 0;
            _dbContext.WatchTasks.RemoveRange(tasks);
            var count = await _dbContext.SaveChangesAsync();
            // Invalidate settings cache after deleting all tasks
            _cache.Remove(SettingsCacheKeys.UserSettingsSingleton);
            return count;
        }
    }
}
