using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class SQLiteSettingsRepository(AppDbContext _dbContext) : ISettingsRepository
    {
        public async Task<UserSettings> LoadAsync()
        {
            try
            {
                var settings = await _dbContext.UserSettings.Include(u => u.EmailSettings).FirstOrDefaultAsync();
                if (settings == null)
                {
                    Log.Warning("No settings found in database, returning default settings.");
                    return new UserSettings(
                        emailRecipient: string.Empty,
                        emailSettingsSenderEmail: string.Empty
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
                var existing = await _dbContext.UserSettings.Include(u => u.EmailSettings).FirstOrDefaultAsync(u => u.EmailRecipient == settings.EmailRecipient);
                if (existing == null)
                {
                    await _dbContext.UserSettings.AddAsync(settings);
                }
                else
                {
                    _dbContext.Entry(existing).CurrentValues.SetValues(settings);
                    if (settings.EmailSettings != null && existing.EmailSettings != null)
                    {
                        _dbContext.Entry(existing.EmailSettings!).CurrentValues.SetValues(settings.EmailSettings);
                    }
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
