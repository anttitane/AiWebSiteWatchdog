using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace AiWebSiteWatchDog.Infrastructure.Auth
{
    public interface IGoogleCredentialProvider
    {
        Task<UserCredential> GetGmailAndGeminiCredentialAsync(string senderEmail, CancellationToken ct = default);
        // Overloads to allow dynamic inclusion of Gmail scope
        Task<UserCredential> GetCredentialAsync(string senderEmail, bool includeGmailSend, CancellationToken ct = default);
        // Scope inspection
        Task<bool> HasGmailSendScopeAsync(string senderEmail, CancellationToken ct = default);
        // Web-based OAuth helpers
        string CreateAuthorizationUrl(string senderEmail, string redirectUri, string? state = null);
        string CreateAuthorizationUrl(string senderEmail, string redirectUri, bool includeGmailSend, string? state = null);
        Task<UserCredential> ExchangeCodeForTokenAsync(string senderEmail, string code, string redirectUri, CancellationToken ct = default);
        Task<UserCredential> ExchangeCodeForTokenAsync(string senderEmail, string code, string redirectUri, bool includeGmailSend, CancellationToken ct = default);
    }
}
