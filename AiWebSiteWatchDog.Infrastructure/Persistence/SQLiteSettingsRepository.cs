using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class SQLiteSettingsRepository(AppDbContext _dbContext) : ISettingsRepository
    {
        public async Task<UserSettings> LoadAsync()
        {
            try
            {
                var settings = await _dbContext.UserSettings
                    .Include(u => u.WatchTasks)
                    .FirstOrDefaultAsync();
                if (settings == null)
                {
                    Log.Warning("No settings found in database, returning default settings.");
                    return new UserSettings(
                        userEmail: string.Empty,
                        senderEmail: string.Empty,
                        senderName: string.Empty
                    );
                }
                Log.Information("Settings loaded successfully from database.");
                return settings;
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "Failed to load settings from database");
                throw;
            }
        }

        public async Task SaveAsync(UserSettings settings)
        {
            try
            {
                // Enforce single-settings-row semantics (one user configuration)
                var existing = await _dbContext.UserSettings.FirstOrDefaultAsync();
                if (existing == null)
                {
                    await _dbContext.UserSettings.AddAsync(settings);
                }
                else
                {
                    // If primary key (UserEmail) changes, propagate to related WatchTasks
                    if (existing.UserEmail != settings.UserEmail)
                    {
                        var oldKey = existing.UserEmail;
                        var tasks = await _dbContext.WatchTasks.Where(w => w.UserSettingsId == oldKey).ToListAsync();
                        foreach (var t in tasks)
                        {
                            t.UserSettingsId = settings.UserEmail;
                        }
                        existing.UserEmail = settings.UserEmail; // update PK after FKs adjusted
                    }
                    existing.SenderEmail = settings.SenderEmail;
                    existing.SenderName = settings.SenderName;
                }
                await _dbContext.SaveChangesAsync();
                Log.Information("Settings saved successfully to database.");
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "Failed to save settings to database");
                throw;
            }
        }
    }
}
