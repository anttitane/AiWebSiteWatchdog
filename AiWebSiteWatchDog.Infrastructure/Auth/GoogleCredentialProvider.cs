using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Auth
{
    /// <summary>
    /// Provides unified Google OAuth2 credentials (Gmail + Gemini) using a persisted refresh token.
    /// Scopes are combined so a single consent covers both services. If scopes change, user must re-consent once.
    /// Token persistence uses a per-user hashed directory to avoid leaking raw email in filesystem paths.
    /// </summary>
    public class GoogleCredentialProvider : IGoogleCredentialProvider
    {
        private static readonly string[] Scopes =
        [
            GmailService.Scope.GmailSend,
            // Gemini / Generative Language broad scope
            "https://www.googleapis.com/auth/generative-language"
        ];

        public async Task<UserCredential> GetGmailAndGeminiCredentialAsync(string senderEmail, string clientSecretJson, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(clientSecretJson))
                throw new ArgumentException("clientSecretJson must be provided", nameof(clientSecretJson));

            try
            {
                using var secretStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clientSecretJson));
                var secrets = GoogleClientSecrets.FromStream(secretStream).Secrets;

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

                // NOTE: For higher security you could:
                // - Encrypt token files at rest (e.g., DPAPI on Windows, libsodium elsewhere)
                // - Store in DB with encryption rather than filesystem
                // - Restrict directory permissions (chmod 700 on Linux) – caller should ensure deployment script handles this.
                // Re-consent scenario: if Gemini scope was added after initial Gmail consent, delete this folder once to force new consent.

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    Scopes,
                    senderEmail,
                    ct,
                    new FileDataStore(tokenPath, true)
                );

                return credential;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to acquire Google credential for {SenderEmail}", senderEmail);
                throw;
            }
        }
    }
}
