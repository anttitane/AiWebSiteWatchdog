using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace AiWebSiteWatchDog.Infrastructure.Auth
{
    public interface IGoogleCredentialProvider
    {
    Task<UserCredential> GetGmailAndGeminiCredentialAsync(string senderEmail, CancellationToken ct = default);
    }
}
