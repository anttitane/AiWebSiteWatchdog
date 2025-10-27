using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Serilog;
using AiWebSiteWatchDog.Domain.Interfaces;
using AiWebSiteWatchDog.Infrastructure.Auth;
using AiWebSiteWatchDog.Domain.Constants;

namespace AiWebSiteWatchDog.Infrastructure.Gemini
{
    public class GeminiApiClient(IGoogleCredentialProvider credentialProvider, ISettingsService settingsService, HttpClient http) : IGeminiApiClient
    {
        private readonly IGoogleCredentialProvider _credentialProvider = credentialProvider;
        private readonly ISettingsService _settingsService = settingsService;
        private readonly HttpClient _http = http;
        public async Task<string> CheckInterestAsync(string url, string prompt)
        {
            // 1. Fetch site
            var html = await _http.GetStringAsync(url);

            // 2. Strip HTML to plain text
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var text = HtmlAgilityPack.HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);

            // 3. Resolve Gemini API endpoint and sender settings with a single read
            var settings = await _settingsService.GetSettingsAsync();
            if (settings is null)
            {
                throw new InvalidOperationException("Application settings not available.");
            }
            var s = settings!; // null-guarded above
            var geminiUrl = string.IsNullOrWhiteSpace(s.GeminiApiUrl) ? GeminiDefaults.ApiUrl : s.GeminiApiUrl;

            // Validate sender email now to avoid re-fetching settings later
            var senderEmail = string.IsNullOrWhiteSpace(s.SenderEmail)
                ? throw new InvalidOperationException("SenderEmail not configured in database.")
                : s.SenderEmail;

            var geminiBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = $"Here is website text:\n{text}\n\n{prompt}\n\n" }
                        }
                    }
                }
            };

            // 4. Call Gemini API
            try
            {
                var credential = await _credentialProvider.GetGmailAndGeminiCredentialAsync(senderEmail);
                var accessToken = await credential.GetAccessTokenForRequestAsync();

                var request = new HttpRequestMessage(HttpMethod.Post, geminiUrl)
                {
                    Content = JsonContent.Create(geminiBody)
                };
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");

                Log.Information("Calling Gemini API at {GeminiUrl} with prompt '{Prompt}'", geminiUrl, prompt);
                var response = await _http.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                Log.Information("Gemini API response received for prompt '{Prompt}'", prompt);
                return json;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Gemini API call failed for prompt '{Prompt}'", prompt);
                throw;
            }
        }
    }
}
