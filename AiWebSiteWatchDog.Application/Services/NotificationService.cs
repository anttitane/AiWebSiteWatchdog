using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using System;

namespace AiWebSiteWatchDog.Application.Services
{
    public class NotificationService(IEmailSender emailSender, ISettingsService settingsService) : INotificationService
    {
        private readonly IEmailSender _emailSender = emailSender;
        private readonly ISettingsService _settingsService = settingsService;

        public async Task SendNotificationAsync(Notification notification)
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.SenderEmail))
            {
                throw new ArgumentNullException(nameof(settings.SenderEmail), "SenderEmail cannot be empty when sending notification.");
            }
            await _emailSender.SendAsync(notification, settings, settings.UserEmail);
        }
    }
}
