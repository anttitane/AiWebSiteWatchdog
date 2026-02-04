using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using AiWebSiteWatchDog.Domain.Constants;
using Microsoft.Extensions.Configuration;
using System;
using AiWebSiteWatchDog.Infrastructure.Auth;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class SQLiteSettingsRepository : ISettingsRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly bool _encryptionEnabled;
        private readonly byte[]? _encryptionKey;

        public SQLiteSettingsRepository(AppDbContext dbContext, IMemoryCache cache, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _cache = cache;
            try
            {
                var keyB64 = Environment.GetEnvironmentVariable("TELEGRAM_TOKENS_ENCRYPTION_KEY")
                    ?? Environment.GetEnvironmentVariable("GOOGLE_TOKENS_ENCRYPTION_KEY")
                    ?? configuration["TELEGRAM_TOKENS_ENCRYPTION_KEY"]
                    ?? configuration["GOOGLE_TOKENS_ENCRYPTION_KEY"];
                if (!string.IsNullOrWhiteSpace(keyB64))
                {
                    var raw = Convert.FromBase64String(keyB64.Trim());
                    if (raw.Length is 16 or 24 or 32)
                    {
                        _encryptionKey = raw;
                        _encryptionEnabled = true;
                        Log.Information("TelegramBotToken encryption enabled (AES-{Size} bytes).", raw.Length);
                    }
                    else
                    {
                        Log.Warning("Encryption key length {Length} invalid for AES; expected 16/24/32. TelegramBotToken encryption disabled.", raw.Length);
                        _encryptionEnabled = false;
                    }
                }
                else
                {
                    _encryptionEnabled = false;
                    Log.Information("No encryption key found; TelegramBotToken stored in plaintext. Set TELEGRAM_TOKENS_ENCRYPTION_KEY or GOOGLE_TOKENS_ENCRYPTION_KEY to enable.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to initialize TelegramBotToken encryption; storing plaintext.");
                _encryptionEnabled = false;
            }
        }

        private string? Encrypt(string? plain)
        {
            if (!_encryptionEnabled || string.IsNullOrWhiteSpace(plain)) return plain;
            try
            {
                return EncryptionHelper.Encrypt(plain, _encryptionKey!);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to encrypt TelegramBotToken");
                return plain; // fallback store plaintext
            }
        }

        private string? Decrypt(string? cipher)
        {
            if (!_encryptionEnabled || string.IsNullOrWhiteSpace(cipher)) return cipher;
            try
            {
                return EncryptionHelper.Decrypt(cipher, _encryptionKey!);
            }
            catch
            {
                // If decryption fails treat as plaintext (legacy row or key rotation mismatch)
                return cipher;
            }
        }
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
                // Decrypt TelegramBotToken if encrypted
                if (!string.IsNullOrWhiteSpace(settings.TelegramBotToken))
                {
                    settings.TelegramBotToken = Decrypt(settings.TelegramBotToken);
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
                    var toAdd = new UserSettings(settings.UserEmail, settings.SenderEmail, settings.SenderName)
                    {
                        GeminiApiUrl = string.IsNullOrWhiteSpace(settings.GeminiApiUrl) ? GeminiDefaults.ApiUrl : settings.GeminiApiUrl,
                        NotificationChannel = settings.NotificationChannel,
                        TelegramBotToken = Encrypt(settings.TelegramBotToken),
                        TelegramChatId = settings.TelegramChatId,
                        NotificationRetentionDays = settings.NotificationRetentionDays,
                        WatchTasks = settings.WatchTasks
                    };
                    await _dbContext.UserSettings.AddAsync(toAdd);
                    existing = toAdd;
                }
                else
                {
                    if (existing.UserEmail != settings.UserEmail)
                    {
                        var oldKey = existing.UserEmail;
                        var tasks = await _dbContext.WatchTasks.Where(w => w.UserSettingsId == oldKey).ToListAsync();
                        foreach (var t in tasks) t.UserSettingsId = settings.UserEmail;
                        existing.UserEmail = settings.UserEmail;
                    }
                    existing.SenderEmail = settings.SenderEmail;
                    existing.SenderName = settings.SenderName;
                    existing.GeminiApiUrl = string.IsNullOrWhiteSpace(settings.GeminiApiUrl) ? GeminiDefaults.ApiUrl : settings.GeminiApiUrl;
                    existing.NotificationChannel = settings.NotificationChannel;
                    existing.TelegramBotToken = Encrypt(settings.TelegramBotToken);
                    existing.TelegramChatId = settings.TelegramChatId;
                    existing.NotificationRetentionDays = settings.NotificationRetentionDays;
                }
                await _dbContext.SaveChangesAsync();
                // After persistence, ensure plaintext token for runtime use
                if (existing != null && _encryptionEnabled && !string.IsNullOrWhiteSpace(settings.TelegramBotToken))
                {
                    existing.TelegramBotToken = settings.TelegramBotToken;
                }
                _cache.Set(SettingsCacheKeys.UserSettingsSingleton, existing);
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
