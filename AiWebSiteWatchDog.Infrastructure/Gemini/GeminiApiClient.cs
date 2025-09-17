using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using Serilog;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Infrastructure.Gemini
{
    public class GeminiApiClient : IGeminiApiClient
    {
        public async Task<string> CheckInterestAsync(string text, string interest, string apiKey)
        {
            var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var geminiBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = $"Here is website text:\n{text}\n\nMy interest is: {interest}\n\nDoes this contain anything interesting? Answer yes or no and explain shortly." }
                        }
                    }
                }
            };

            try
            {
                using var http = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, geminiUrl)
                {
                    Content = JsonContent.Create(geminiBody)
                };
                request.Headers.Add("X-goog-api-key", apiKey);
                request.Headers.Add("Accept", "application/json");

                Log.Information("Calling Gemini API for interest '{Interest}'", interest);
                var response = await http.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                Log.Information("Gemini API response received for interest '{Interest}'", interest);
                return json;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Gemini API call failed for interest '{Interest}'", interest);
                throw;
            }
        }
    }
}
