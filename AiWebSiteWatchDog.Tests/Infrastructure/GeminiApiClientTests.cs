using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using AiWebSiteWatchDog.Infrastructure.Auth;
using AiWebSiteWatchDog.Infrastructure.Gemini;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Moq;
using Xunit;

namespace AiWebSiteWatchDog.Tests.Infrastructure;

public class GeminiApiClientTests
{
    private sealed class TestHandler : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }
        public string? LastAuthHeader { get; private set; }
        public bool ForcePostFailure { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.ToString() == "https://site.example/")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><body>alpha beta</body></html>")
                };
            }

            if (request.Method == HttpMethod.Post && request.RequestUri!.ToString() == "https://gemini.test/api")
            {
                LastAuthHeader = request.Headers.Authorization?.ToString();
                LastRequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                if (ForcePostFailure)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("error")
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"candidates\":[]}")
                    };
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    private static UserCredential CreateFakeCredential()
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = "id", ClientSecret = "secret" },
            Scopes = ["scope"]
        });
        var token = new TokenResponse
        {
            AccessToken = "TEST_TOKEN",
            ExpiresInSeconds = 3600,
            IssuedUtc = DateTime.UtcNow
        };
        return new UserCredential(flow, "user", token);
    }

    [Fact]
    public async Task CheckInterestAsync_SendsBearerToken_AndBuildsBodyFromSiteAndPrompt()
    {
        var handler = new TestHandler();
        var http = new HttpClient(handler);
        var credential = CreateFakeCredential();

        var credProvider = new Mock<IGoogleCredentialProvider>();
        credProvider.Setup(c => c.GetGmailAndGeminiCredentialAsync("sender@example.com", default)).ReturnsAsync(credential);

        var settingsSvc = new Mock<ISettingsService>();
        var settings = new UserSettings("user@example.com", "sender@example.com", "Sender")
        {
            GeminiApiUrl = "https://gemini.test/api"
        };
        settingsSvc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

        var client = new GeminiApiClient(credProvider.Object, settingsSvc.Object, http);
        var json = await client.CheckInterestAsync("https://site.example/", "find alpha");

        json.Should().Contain("candidates");
        handler.LastAuthHeader.Should().Be("Bearer TEST_TOKEN");
        handler.LastRequestBody.Should().Contain("alpha beta");
        handler.LastRequestBody.Should().Contain("find alpha");
    }

    [Fact]
    public async Task CheckInterestAsync_NonSuccessFromGemini_Throws()
    {
        var handler = new TestHandler { ForcePostFailure = true };
        var http = new HttpClient(handler);
        var credential = CreateFakeCredential();

        var credProvider = new Mock<IGoogleCredentialProvider>();
        credProvider.Setup(c => c.GetGmailAndGeminiCredentialAsync("sender@example.com", default)).ReturnsAsync(credential);

        var settingsSvc = new Mock<ISettingsService>();
        var settings = new UserSettings("user@example.com", "sender@example.com", "Sender")
        {
            GeminiApiUrl = "https://gemini.test/api"
        };
        settingsSvc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

        var client = new GeminiApiClient(credProvider.Object, settingsSvc.Object, http);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.CheckInterestAsync("https://site.example/", "any"));
    }

    [Fact]
    public async Task CheckInterestAsync_MissingSenderEmail_Throws()
    {
        var handler = new TestHandler();
        var http = new HttpClient(handler);

        var credProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict); // should not be called

        var settingsSvc = new Mock<ISettingsService>();
        var settings = new UserSettings("user@example.com", " ", "Sender")
        {
            GeminiApiUrl = "https://gemini.test/api"
        };
        settingsSvc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

        var client = new GeminiApiClient(credProvider.Object, settingsSvc.Object, http);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.CheckInterestAsync("https://site.example/", "any"));
        ex.Message.Should().Contain("SenderEmail not configured");
        credProvider.VerifyNoOtherCalls();
    }
}
