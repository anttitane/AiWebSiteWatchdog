using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Application.Services;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace AiWebSiteWatchDog.Tests.Application;

public class WatcherServiceTests
{
    [Fact]
    public async Task CheckWebsiteAsync_UpdatesTaskFields()
    {
        var gemini = new Mock<IGeminiApiClient>();
        gemini.Setup(g => g.CheckInterestAsync("https://example.com", "prompt"))
              .ReturnsAsync("gemini-response");
        var settings = new Mock<ISettingsService>(); // not used now but kept for future expansion

        var svc = new WatcherService(gemini.Object, settings.Object);
        var task = new WatchTask { Id = 1, Url = "https://example.com", TaskPrompt = "prompt", Title = "T" };
        var before = DateTime.UtcNow;
        var updated = await svc.CheckWebsiteAsync(task);
        updated.LastResult.Should().Be("gemini-response");
        updated.LastChecked.Should().BeOnOrAfter(before);
    }
}
