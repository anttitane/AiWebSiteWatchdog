using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Infrastructure.Gemini
{
    public class GeminiApiClient
    {
        public async Task<string> CheckInterestAsync(string text, string interest, string apiKey)
        {
            // TODO: Implement Gemini API call
            await Task.Delay(100); // Simulate async
            return "Stub: Gemini API response";
        }
    }
}
