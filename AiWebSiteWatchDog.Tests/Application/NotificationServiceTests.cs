using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Application.Services;
using AiWebSiteWatchDog.Domain.DTOs;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace AiWebSiteWatchDog.Tests.Application;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendNotificationAsync_SendsEmailAndPersistsNotification()
    {
        var emailSender = new Mock<IEmailSender>();
        var telegramSender = new Mock<ITelegramSender>();
        var settingsService = new Mock<ISettingsService>();
        var repo = new Mock<INotificationRepository>();

        var settings = new UserSettings("user@example.com", "sender@example.com", "S");
        settingsService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
        repo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
        emailSender.Setup(e => e.SendAsync(It.IsAny<Notification>(), settings, settings.UserEmail))
                   .Returns(Task.CompletedTask)
                   .Verifiable();

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, emailSender.Object, telegramSender.Object, settingsService.Object, repo.Object);
        var dto = await svc.SendNotificationAsync(new CreateNotificationRequest("Subject", "Message"));

        dto.Subject.Should().Be("Subject");
        dto.Message.Should().Be("Message");
        dto.Id.Should().Be(0); // unchanged before persistence layer sets it
        emailSender.Verify();
        repo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Subject == "Subject" && n.Message == "Message")), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_Throws_WhenSenderEmailMissing()
    {
        var emailSender = new Mock<IEmailSender>();
        var telegramSender = new Mock<ITelegramSender>();
        var settingsService = new Mock<ISettingsService>();
        var repo = new Mock<INotificationRepository>();
        settingsService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new UserSettings("user@example.com", "  ", "S"));
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, emailSender.Object, telegramSender.Object, settingsService.Object, repo.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SendNotificationAsync(new CreateNotificationRequest("s", "m")));
    }

    [Fact]
    public async Task SendNotificationAsync_WhenEmailSendFails_DoesNotPersistAndBubbles()
    {
        var emailSender = new Mock<IEmailSender>();
        var telegramSender = new Mock<ITelegramSender>();
        var settingsService = new Mock<ISettingsService>();
        var repo = new Mock<INotificationRepository>();

        var settings = new UserSettings("user@example.com", "sender@example.com", "S");
        settingsService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

        emailSender
            .Setup(e => e.SendAsync(It.IsAny<Notification>(), settings, settings.UserEmail))
            .ThrowsAsync(new InvalidOperationException("smtp-fail"));

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, emailSender.Object, telegramSender.Object, settingsService.Object, repo.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SendNotificationAsync(new CreateNotificationRequest("s", "m")));
        repo.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
    }
}
