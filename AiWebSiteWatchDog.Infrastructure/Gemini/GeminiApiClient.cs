using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Serilog;
using AiWebSiteWatchDog.Domain.Interfaces;
using AiWebSiteWatchDog.Infrastructure.Auth;
using Google.Apis.Auth.OAuth2;

namespace AiWebSiteWatchDog.Infrastructure.Gemini
{
    public class GeminiApiClient(IGoogleCredentialProvider credentialProvider, ISettingsService settingsService) : IGeminiApiClient
    {
        private readonly IGoogleCredentialProvider _credentialProvider = credentialProvider;
        private readonly ISettingsService _settingsService = settingsService;
        public async Task<string> CheckInterestAsync(string text, string interest)
        {
            var geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

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
                var settings = await _settingsService.GetSettingsAsync();
                var senderEmail = settings.EmailSettings?.SenderEmail
                    ?? throw new InvalidOperationException("EmailSettings.SenderEmail not configured in database.");
                var clientSecretJson = settings.EmailSettings?.GmailClientSecretJson
                    ?? throw new InvalidOperationException("EmailSettings.GmailClientSecretJson not configured in database.");

                var credential = await _credentialProvider.GetGmailAndGeminiCredentialAsync(senderEmail, clientSecretJson);
                var accessToken = await credential.GetAccessTokenForRequestAsync();

                using var http = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, geminiUrl)
                {
                    Content = JsonContent.Create(geminiBody)
                };
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
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
