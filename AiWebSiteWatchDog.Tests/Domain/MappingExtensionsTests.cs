using System;
using System.Collections.Generic;
using System.Linq;
using AiWebSiteWatchDog.Domain.DTOs;
using AiWebSiteWatchDog.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AiWebSiteWatchDog.Tests.Domain;

public class MappingExtensionsTests
{
    [Fact]
    public void WatchTask_ToDto_MapsAllFields()
    {
        var now = DateTime.UtcNow;
        var task = new WatchTask
        {
            Id = 42,
            Title = "t",
            Url = "https://example.com",
            TaskPrompt = "p",
            Schedule = "*/15 * * * *",
            LastChecked = now,
            LastResult = "result",
            Enabled = true
        };
        var dto = task.ToDto();
        dto.Id.Should().Be(42);
        dto.Title.Should().Be("t");
        dto.Url.Should().Be("https://example.com");
        dto.TaskPrompt.Should().Be("p");
        dto.Schedule.Should().Be("*/15 * * * *");
        dto.LastChecked.Should().Be(now);
        dto.LastResult.Should().Be("result");
        dto.Enabled.Should().BeTrue();
    }

    [Fact]
    public void UserSettings_ToDto_IncludesTaskSummaries()
    {
        var settings = new UserSettings("user@example.com", "sender@example.com", "Sender")
        {
            GeminiApiUrl = "https://gemini",
            WatchTasks = new List<WatchTask>
            {
                new() { Id = 1, Title = "A", Url = "u1", TaskPrompt = "p1", Schedule = "* * * * *", Enabled = true },
                new() { Id = 2, Title = "B", Url = "u2", TaskPrompt = "p2", Schedule = "*/5 * * * *", Enabled = false }
            }
        };
        var dto = settings.ToDto();
        dto.UserEmail.Should().Be("user@example.com");
        dto.SenderEmail.Should().Be("sender@example.com");
        dto.SenderName.Should().Be("Sender");
        dto.GeminiApiUrl.Should().Be("https://gemini");
        dto.WatchTasks.Should().HaveCount(2);
        var first = dto.WatchTasks.First();
        first.Id.Should().Be(1);
        first.Title.Should().Be("A");
        first.Url.Should().Be("u1");
        first.Schedule.Should().Be("* * * * *");
        first.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Notification_ToDto_MapsFields()
    {
        var sentAt = DateTime.UtcNow;
        var n = new Notification(5, "subj", "body", sentAt);
        var dto = n.ToDto();
        dto.Id.Should().Be(5);
        dto.Subject.Should().Be("subj");
        dto.Message.Should().Be("body");
        dto.SentAt.Should().Be(sentAt);
    }
}
