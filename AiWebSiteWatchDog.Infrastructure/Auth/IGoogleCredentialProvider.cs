using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace AiWebSiteWatchDog.Infrastructure.Auth
{
    public interface IGoogleCredentialProvider
    {
        Task<UserCredential> GetGmailAndGeminiCredentialAsync(string senderEmail, CancellationToken ct = default);
        // Web-based OAuth helpers
        string CreateAuthorizationUrl(string senderEmail, string redirectUri, string? state = null);
        Task<UserCredential> ExchangeCodeForTokenAsync(string senderEmail, string code, string redirectUri, CancellationToken ct = default);
    }
}
