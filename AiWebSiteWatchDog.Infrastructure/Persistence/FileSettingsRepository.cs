using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class FileSettingsRepository
    {
        private readonly string _filePath = "settings.json";

        public async Task<UserSettings> LoadAsync()
        {
            if (!File.Exists(_filePath))
                return new UserSettings();
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }

        public async Task SaveAsync(UserSettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
