using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class FileSettingsRepository
    {
        private readonly string _filePath = "settings.json";

        public async Task<UserSettings> LoadAsync()
        {
            try
            {
                Log.Information("Loading settings from {FilePath}", _filePath);
                if (!File.Exists(_filePath))
                {
                    Log.Warning("Settings file {FilePath} does not exist, returning default settings.", _filePath);
                    return new UserSettings();
                }
                var json = await File.ReadAllTextAsync(_filePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                Log.Information("Settings loaded successfully from {FilePath}", _filePath);
                return settings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings from {FilePath}", _filePath);
                throw;
            }
        }

        public async Task SaveAsync(UserSettings settings)
        {
            try
            {
                Log.Information("Saving settings to {FilePath}", _filePath);
                var json = JsonSerializer.Serialize(settings);
                await File.WriteAllTextAsync(_filePath, json);
                Log.Information("Settings saved successfully to {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings to {FilePath}", _filePath);
                throw;
            }
        }
    }
}
