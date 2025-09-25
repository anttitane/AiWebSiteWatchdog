using System;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using AiWebSiteWatchDog.Infrastructure.Persistence;

namespace AiWebSiteWatchDog.Infrastructure.Auth
{
    /// <summary>
    /// IDataStore implementation persisting Google token JSON encrypted in the database.
    /// </summary>
    internal class DbEncryptedDataStore : IDataStore
    {
        private readonly AppDbContext _dbContext;
        private readonly byte[] _key;

        public DbEncryptedDataStore(AppDbContext dbContext, byte[] key)
        {
            _dbContext = dbContext;
            _key = key;
        }

        private static string NormalizeKey(string key) => key; // Could namespace by scopes later

        public async Task StoreAsync<T>(string key, T value)
        {
            var norm = NormalizeKey(key);
            var json = JsonSerializer.Serialize(value);
            var encrypted = EncryptionHelper.Encrypt(json, _key);
            var existing = await _dbContext.GoogleOAuthTokens.FirstOrDefaultAsync(t => t.Email == norm);
            if (existing == null)
            {
                _dbContext.GoogleOAuthTokens.Add(new GoogleOAuthToken
                {
                    Email = norm,
                    EncryptedJson = encrypted,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.EncryptedJson = encrypted;
                existing.UpdatedUtc = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync<T>(string key)
        {
            var norm = NormalizeKey(key);
            var existing = await _dbContext.GoogleOAuthTokens.FirstOrDefaultAsync(t => t.Email == norm);
            if (existing != null)
            {
                _dbContext.GoogleOAuthTokens.Remove(existing);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var norm = NormalizeKey(key);
            var existing = await _dbContext.GoogleOAuthTokens.FirstOrDefaultAsync(t => t.Email == norm);
            if (existing == null) return default!;
            try
            {
                var json = EncryptionHelper.Decrypt(existing.EncryptedJson, _key);
                return JsonSerializer.Deserialize<T>(json)!;
            }
            catch
            {
                // Corruption or key rotation mismatch; treat as missing to force re-auth.
                return default!;
            }
        }

        public async Task ClearAsync()
        {
            _dbContext.GoogleOAuthTokens.RemoveRange(_dbContext.GoogleOAuthTokens);
            await _dbContext.SaveChangesAsync();
        }
    }
}
