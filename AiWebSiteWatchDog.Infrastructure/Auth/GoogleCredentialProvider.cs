using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Serilog;
using Microsoft.Extensions.Configuration;
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
        private static readonly string GeminiScope = "https://www.googleapis.com/auth/generative-language.retriever";

        private static string[] BuildScopes(bool includeGmailSend)
        {
            return includeGmailSend
                ? [GmailService.Scope.GmailSend, GeminiScope]
                : [GeminiScope];
        }

    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly bool _useDbStore;
    private readonly byte[]? _encryptionKey;

        public GoogleCredentialProvider(AppDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _config = config;
            // Read USE_DB_TOKEN_STORE preferring environment variable, then IConfiguration (user-secrets/appsettings).
            var useDbRaw = Environment.GetEnvironmentVariable("USE_DB_TOKEN_STORE");
            if (string.IsNullOrWhiteSpace(useDbRaw))
                useDbRaw = _config["USE_DB_TOKEN_STORE"];

            bool useDb = false;
            if (!string.IsNullOrWhiteSpace(useDbRaw))
            {
                var trimmed = useDbRaw.Trim();
                // Accept true/false (bool.TryParse), and common synonyms like "1", "yes".
                if (!bool.TryParse(trimmed, out useDb))
                {
                    useDb = trimmed == "1" || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("y", StringComparison.OrdinalIgnoreCase);
                }
            }

            _useDbStore = useDb;
            if (_useDbStore)
            {
                // Prefer environment variable, fall back to configuration (user-secrets / appsettings)
                var keyB64 = Environment.GetEnvironmentVariable("GOOGLE_TOKENS_ENCRYPTION_KEY")
                    ?? _config["GOOGLE_TOKENS_ENCRYPTION_KEY"]
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
            return await GetCredentialAsync(senderEmail, includeGmailSend: true, ct);
        }

        public async Task<UserCredential> GetCredentialAsync(string senderEmail, bool includeGmailSend, CancellationToken ct = default)
        {
            // Headless/server-safe credential acquisition:
            //  - Never launches a browser. Tokens must be pre-seeded via /auth/start + /auth/callback flow.
            //  - Attempts refresh; if refresh token is missing or revoked (invalid_grant) we purge and instruct re-consent.

            var clientSecretJson = ResolveClientSecretJson();
            using var secretStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clientSecretJson));
            var secrets = GoogleClientSecrets.FromStream(secretStream).Secrets;

            IDataStore dataStore = _useDbStore
                ? new DbEncryptedDataStore(_dbContext, _encryptionKey!)
                : CreateFileStore(senderEmail);

            // Build flow (same scopes so a single consent covers Gmail + Gemini)
            // Note: Do not dispose the flow - UserCredential needs it for token refresh operations
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = BuildScopes(includeGmailSend),
                DataStore = dataStore
            });

            // Load stored token (created previously by ExchangeCodeForTokenAsync). If absent, instruct re-consent.
            var token = await flow.LoadTokenAsync(senderEmail, ct);
            if (token == null)
            {
                throw new InvalidOperationException("No stored Google OAuth token found for sender email. Visit /auth to (re)authorize.");
            }
            if (string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                // A token without a refresh token cannot be refreshed in headless mode.
                await dataStore.DeleteAsync<TokenResponse>(senderEmail);
                throw new InvalidOperationException("Stored token missing refresh token. Re-authorize via /auth to obtain offline access.");
            }

            var credential = new UserCredential(flow, senderEmail, token);
            try
            {
                // Attempt refresh so we always return a valid access token.
                if (credential.Token.IsStale)
                {
                    var refreshed = await credential.RefreshTokenAsync(ct);
                    if (!refreshed)
                    {
                        await dataStore.DeleteAsync<TokenResponse>(senderEmail);
                        throw new InvalidOperationException("Failed to refresh Google OAuth token. Re-authorize via /auth.");
                    }
                }
                return credential;
            }
            catch (TokenResponseException tex) when (tex.Error?.Error == "invalid_grant")
            {
                // Refresh token has been revoked / expired => purge and require new consent.
                await dataStore.DeleteAsync<TokenResponse>(senderEmail);
                Log.Warning(tex, "Google refresh token invalid_grant for {SenderEmail}. Purged stored token.");
                throw new InvalidOperationException("Google OAuth refresh token revoked or expired. Re-authorize via /auth to continue.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to acquire Google credential for {SenderEmail}", senderEmail);
                throw;
            }
        }

        /// <summary>
        /// Generates a Google OAuth2 authorization URL for web-based consent (server redirect flow).
        /// Use together with <see cref="ExchangeCodeForTokenAsync"/> in the callback to store the tokens.
        /// </summary>
        public string CreateAuthorizationUrl(string senderEmail, string redirectUri, string? state = null)
        {
            return CreateAuthorizationUrl(senderEmail, redirectUri, includeGmailSend: true, state);
        }

        public string CreateAuthorizationUrl(string senderEmail, string redirectUri, bool includeGmailSend, string? state = null)
        {
            // Resolve secrets (supports file/B64/raw env or IConfiguration)
            var clientSecretJson = ResolveClientSecretJson();
            using var secretStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clientSecretJson));
            var secrets = GoogleClientSecrets.FromStream(secretStream).Secrets;

            IDataStore dataStore = _useDbStore
                ? new DbEncryptedDataStore(_dbContext, _encryptionKey!)
                : CreateFileStore(senderEmail);

            using var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = BuildScopes(includeGmailSend),
                DataStore = dataStore
            });
            
            var url = flow.CreateAuthorizationCodeRequest(redirectUri);
            if (!string.IsNullOrWhiteSpace(state)) url.State = state;
            var built = url.Build().ToString();
            built = ReplaceOrAddQueryParam(built, "access_type", "offline");
            built = ReplaceOrAddQueryParam(built, "prompt", "consent");
            built = ReplaceOrAddQueryParam(built, "include_granted_scopes", "true");
            return built;
        }

        /// <summary>
        /// Exchanges the authorization code for tokens and persists them via the configured IDataStore.
        /// Returns a usable UserCredential.
        /// </summary>
        public async Task<UserCredential> ExchangeCodeForTokenAsync(string senderEmail, string code, string redirectUri, CancellationToken ct = default)
        {
            return await ExchangeCodeForTokenAsync(senderEmail, code, redirectUri, includeGmailSend: true, ct);
        }

        public async Task<UserCredential> ExchangeCodeForTokenAsync(string senderEmail, string code, string redirectUri, bool includeGmailSend, CancellationToken ct = default)
        {
            var clientSecretJson = ResolveClientSecretJson();
            using var secretStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clientSecretJson));
            var secrets = GoogleClientSecrets.FromStream(secretStream).Secrets;

            IDataStore dataStore = _useDbStore
                ? new DbEncryptedDataStore(_dbContext, _encryptionKey!)
                : CreateFileStore(senderEmail);

            // Note: Do not dispose the flow - UserCredential needs it for token refresh operations
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = BuildScopes(includeGmailSend),
                DataStore = dataStore
            });

            TokenResponse token = await flow.ExchangeCodeForTokenAsync(senderEmail, code, redirectUri, ct);
            return new UserCredential(flow, senderEmail, token);
        }

        public async Task<bool> HasGmailSendScopeAsync(string senderEmail, CancellationToken ct = default)
        {
            // Build minimal flow (Gemini-only) just to access token store; scopes of existing token reflect original consent
            var clientSecretJson = ResolveClientSecretJson();
            using var secretStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clientSecretJson));
            var secrets = GoogleClientSecrets.FromStream(secretStream).Secrets;

            IDataStore dataStore = _useDbStore
                ? new DbEncryptedDataStore(_dbContext, _encryptionKey!)
                : CreateFileStore(senderEmail);

            using var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = BuildScopes(includeGmailSend: false), // minimal
                DataStore = dataStore
            });

            var token = await flow.LoadTokenAsync(senderEmail, ct);
            if (token == null) return false;
            if (string.IsNullOrWhiteSpace(token.Scope)) return false;
            var scopes = token.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return scopes.Contains(GmailService.Scope.GmailSend, StringComparer.OrdinalIgnoreCase);
        }

        private string ResolveClientSecretJson()
        {
            // 1) File path
            var secretFile = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET_JSON_FILE")
                               ?? _config["GOOGLE_CLIENT_SECRET_JSON_FILE"];
            if (!string.IsNullOrWhiteSpace(secretFile) && File.Exists(secretFile))
            {
                return File.ReadAllText(secretFile);
            }
            // 2) Base64
            var secretB64 = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET_JSON_B64")
                             ?? _config["GOOGLE_CLIENT_SECRET_JSON_B64"];
            if (!string.IsNullOrWhiteSpace(secretB64))
            {
                try
                {
                    var data = Convert.FromBase64String(secretB64.Trim());
                    return System.Text.Encoding.UTF8.GetString(data);
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException("GOOGLE_CLIENT_SECRET_JSON_B64 is not a valid Base64 string.", ex);
                }
            }
            // 3) Raw JSON
            var clientSecretJson = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET_JSON")
                                   ?? _config["GOOGLE_CLIENT_SECRET_JSON"];
            if (string.IsNullOrWhiteSpace(clientSecretJson))
                throw new InvalidOperationException("Client secret is required. Provide GOOGLE_CLIENT_SECRET_JSON, or GOOGLE_CLIENT_SECRET_JSON_FILE, or GOOGLE_CLIENT_SECRET_JSON_B64.");
            return clientSecretJson;
        }

        private static string ReplaceOrAddQueryParam(string url, string key, string value)
        {
            var builder = new UriBuilder(url);
            var query = builder.Query; // starts with '?'
            var items = new List<(string k, string v)>();
            if (!string.IsNullOrEmpty(query))
            {
                var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in pairs)
                {
                    var idx = p.IndexOf('=');
                    if (idx >= 0)
                    {
                        var k = Uri.UnescapeDataString(p.Substring(0, idx));
                        var v = Uri.UnescapeDataString(p.Substring(idx + 1));
                        items.Add((k, v));
                    }
                    else if (p.Length > 0)
                    {
                        items.Add((Uri.UnescapeDataString(p), string.Empty));
                    }
                }
            }

            // remove existing key(s) (case-insensitive)
            items.RemoveAll(t => string.Equals(t.k, key, StringComparison.OrdinalIgnoreCase));
            // add desired value
            items.Add((key, value));

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(items[i].k));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(items[i].v));
            }
            builder.Query = sb.ToString();
            return builder.Uri.ToString();
        }

        private static IDataStore CreateFileStore(string senderEmail)
        {
            string tokenPath = GetTokenPath(senderEmail);
            Directory.CreateDirectory(tokenPath);
            return new FileDataStore(tokenPath, true);
        }

        private static string GetTokenPath(string senderEmail)
        {
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
            return tokenPath;
        }
    }
}
