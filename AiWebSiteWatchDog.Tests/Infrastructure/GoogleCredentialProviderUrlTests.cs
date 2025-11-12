using System;
using System.Collections.Generic;
using System.Text;
using AiWebSiteWatchDog.Infrastructure.Auth;
using AiWebSiteWatchDog.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AiWebSiteWatchDog.Tests.Infrastructure;

public class GoogleCredentialProviderUrlTests
{
    [Fact]
    public void CreateAuthorizationUrl_AddsOfflineConsentAndScopes()
    {
        var clientSecret = "{\"installed\":{\"client_id\":\"test\",\"client_secret\":\"secret\",\"redirect_uris\":[\"http://localhost\"]}}";
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientSecret));
        var inMemory = new Dictionary<string, string?>
        {
            ["GOOGLE_CLIENT_SECRET_JSON_B64"] = b64
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory!).Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AppDbContext(options);

        var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        Environment.SetEnvironmentVariable("GOOGLE_TOKENS_PATH", tmp);

        var provider = new GoogleCredentialProvider(db, config);
        var redirect = "https://host/auth/callback";
        var url = provider.CreateAuthorizationUrl("sender@example.com", redirect, state: "abc");

        url.Should().Contain("access_type=offline");
        url.Should().Contain("prompt=consent");
        url.Should().Contain("include_granted_scopes=true");
        url.Should().Contain(Uri.EscapeDataString(redirect));

        // Ensure both scopes are requested
        url.Should().Contain("scope=");
        url.Should().Contain(Uri.EscapeDataString("https://www.googleapis.com/auth/gmail.send"));
        url.Should().Contain(Uri.EscapeDataString("https://www.googleapis.com/auth/generative-language.retriever"));
    }
}
