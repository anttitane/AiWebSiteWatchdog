using System.Threading.Tasks;

namespace AiWebSiteWatchDog.Domain.Interfaces
{
    public interface IGeminiApiClient
    {
        Task<string> CheckInterestAsync(string url, string interest);
    }
}
