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
            if (settings.EmailSettings == null)
            {
                throw new ArgumentNullException(nameof(settings.EmailSettings), "EmailSettings cannot be null when sending notification.");
            }
            await _emailSender.SendAsync(notification, settings.EmailSettings, settings.EmailRecipient);
        }
    }
}
