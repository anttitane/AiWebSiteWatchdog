using System;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    /// <summary>
    /// Encrypted persisted Google OAuth token container. Stores serialized token JSON encrypted.
    /// </summary>
    public class GoogleOAuthToken
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!; // Unique per Google account
        public string EncryptedJson { get; set; } = null!; // Base64(nonce|cipher|tag) or similar format
        public DateTime UpdatedUtc { get; set; }
    }
}
