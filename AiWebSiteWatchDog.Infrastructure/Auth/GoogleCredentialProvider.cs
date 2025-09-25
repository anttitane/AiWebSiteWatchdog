using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Serilog;
using AiWebSiteWatchDog.Infrastructure.Persistence;
using Google.Apis.Util.Store;

namespace AiWebSiteWatchDog.Infrastructure.Auth
{
    /// <summary>
    /// Provides unified Google OAuth2 credentials (Gmail + Gemini) using a persisted refresh token.
    /// Scopes are combined so a single consent covers both services. If scopes change, user must re-consent once.
    /// Token persistence uses either:
    ///  1) Encrypted DB data store (preferred) when USE_DB_TOKEN_STORE=true and GOOGLE_TOKENS_ENCRYPTION_KEY provided.
    ///  2) Hashed per-user filesystem directory fallback (legacy / dev convenience).
    /// Environment variables:
    ///  USE_DB_TOKEN_STORE=true|false (default false)
    ///  GOOGLE_TOKENS_ENCRYPTION_KEY=Base64(AES-256 key) required when DB mode is enabled
    ///  GOOGLE_TOKENS_PATH=override filesystem base directory when not using DB
    /// </summary>
    public class GoogleCredentialProvider : IGoogleCredentialProvider
    {
        private static readonly string[] Scopes =
        [
            GmailService.Scope.GmailSend,
            // Gemini / Generative Language broad scope
            "https://www.googleapis.com/auth/generative-language"
        ];

        private readonly AppDbContext _dbContext;
        private readonly bool _useDbStore;
        private readonly byte[]? _encryptionKey;

        public GoogleCredentialProvider(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _useDbStore = string.Equals(Environment.GetEnvironmentVariable("USE_DB_TOKEN_STORE"), "true", StringComparison.OrdinalIgnoreCase);
            if (_useDbStore)
            {
                var keyB64 = Environment.GetEnvironmentVariable("GOOGLE_TOKENS_ENCRYPTION_KEY")
                    ?? throw new InvalidOperationException("GOOGLE_TOKENS_ENCRYPTION_KEY must be set when USE_DB_TOKEN_STORE=true");
                try
                {
                    _encryptionKey = Convert.FromBase64String(keyB64);
                }
                catch
                {
                    throw new InvalidOperationException("GOOGLE_TOKENS_ENCRYPTION_KEY must be valid Base64");
                }
                if (_encryptionKey!.Length is not 16 and not 24 and not 32)
                    throw new InvalidOperationException("GOOGLE_TOKENS_ENCRYPTION_KEY must decode to 16, 24, or 32 bytes (AES key size)");
            }
        }

        public async Task<UserCredential> GetGmailAndGeminiCredentialAsync(string senderEmail, CancellationToken ct = default)
        {
            var clientSecretJson = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET_JSON");
            if (string.IsNullOrWhiteSpace(clientSecretJson))
                throw new InvalidOperationException("GOOGLE_CLIENT_SECRET_JSON environment variable is required.");

            try
            {
                using var secretStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clientSecretJson));
                var secrets = GoogleClientSecrets.FromStream(secretStream).Secrets;

                IDataStore dataStore = _useDbStore
                    ? new DbEncryptedDataStore(_dbContext, _encryptionKey!)
                    : CreateFileStore(senderEmail);

                // Base token storage path (can be overridden via env variable)
                string tokenPath;
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiWebSiteWatchdog", "GoogleTokens");
                }
                else
                {
                    var home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
                    tokenPath = Path.Combine(home, ".config", "AiWebSiteWatchdog", "GoogleTokens");
                }
                var customPath = Environment.GetEnvironmentVariable("GOOGLE_TOKENS_PATH");
                if (!string.IsNullOrWhiteSpace(customPath))
                    tokenPath = customPath;

                // Use hashed subfolder per email (privacy & avoids special chars). SHA256(email).Take(16) for folder name.
                var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(senderEmail)))[..16];
                tokenPath = Path.Combine(tokenPath, emailHash);
                Directory.CreateDirectory(tokenPath);

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    Scopes,
                    senderEmail,
                    ct,
                    dataStore
                );

                return credential;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to acquire Google credential for {SenderEmail}", senderEmail);
                throw;
            }
        }

        private static IDataStore CreateFileStore(string senderEmail)
        {
            // Base token storage path (can be overridden via env variable)
            string tokenPath;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiWebSiteWatchdog", "GoogleTokens");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
                tokenPath = Path.Combine(home, ".config", "AiWebSiteWatchdog", "GoogleTokens");
            }
            var customPath = Environment.GetEnvironmentVariable("GOOGLE_TOKENS_PATH");
            if (!string.IsNullOrWhiteSpace(customPath))
                tokenPath = customPath;
            var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(senderEmail)))[..16];
            tokenPath = Path.Combine(tokenPath, emailHash);
            Directory.CreateDirectory(tokenPath);
            return new FileDataStore(tokenPath, true);
        }
    }
}
