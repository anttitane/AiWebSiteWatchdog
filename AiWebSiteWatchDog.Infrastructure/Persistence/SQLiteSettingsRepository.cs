using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using AiWebSiteWatchDog.Domain.Constants;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class SQLiteSettingsRepository(AppDbContext _dbContext, IMemoryCache cache) : ISettingsRepository
    {
        private readonly IMemoryCache _cache = cache;
        public async Task<UserSettings> LoadAsync()
        {
            try
            {
                if (_cache.TryGetValue<UserSettings>(SettingsCacheKeys.UserSettingsSingleton, out var cached) && cached is not null)
                {
                    return cached;
                }

                var settings = await _dbContext.UserSettings
                    .Include(u => u.WatchTasks)
                    .OrderBy(u => u.UserEmail)
                    .FirstOrDefaultAsync();
                if (settings == null)
                {
                    Log.Warning("No settings found in database, returning default settings.");
                    var defaults = new UserSettings(
                        userEmail: string.Empty,
                        senderEmail: string.Empty,
                        senderName: string.Empty
                    );
                    // Ensure default Gemini URL is populated
                    if (string.IsNullOrWhiteSpace(defaults.GeminiApiUrl))
                    {
                        defaults.GeminiApiUrl = GeminiDefaults.ApiUrl;
                    }
                    _cache.Set(SettingsCacheKeys.UserSettingsSingleton, defaults);
                    return defaults;
                }
                // Backfill default if the column exists but value is empty (older rows)
                if (string.IsNullOrWhiteSpace(settings.GeminiApiUrl))
                {
                    settings.GeminiApiUrl = GeminiDefaults.ApiUrl;
                }
                Log.Information("Settings loaded successfully from database.");
                _cache.Set(SettingsCacheKeys.UserSettingsSingleton, settings);
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
                var existing = await _dbContext.UserSettings
                    .OrderBy(u => u.UserEmail)
                    .FirstOrDefaultAsync();
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
                    existing.GeminiApiUrl = string.IsNullOrWhiteSpace(settings.GeminiApiUrl)
                        ? GeminiDefaults.ApiUrl
                        : settings.GeminiApiUrl;
                    existing.NotificationChannel = settings.NotificationChannel;
                    existing.TelegramBotToken = settings.TelegramBotToken;
                    existing.TelegramChatId = settings.TelegramChatId;
                }
                await _dbContext.SaveChangesAsync();
                // Update cache after save
                _cache.Set(SettingsCacheKeys.UserSettingsSingleton, existing ?? settings);
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
