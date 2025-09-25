using System.Threading.Tasks;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface IGeminiApiClient
    {
        // Uses stored OAuth credential; no apiKey needed.
        Task<string> CheckInterestAsync(string text, string interest);
    }
}
