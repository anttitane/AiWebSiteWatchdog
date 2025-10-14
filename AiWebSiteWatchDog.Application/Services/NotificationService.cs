using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using System;
using AiWebSiteWatchDog.Domain.DTOs;

namespace AiWebSiteWatchDog.Application.Services
{
    public class NotificationService(IEmailSender emailSender, ISettingsService settingsService) : INotificationService
    {
        private readonly IEmailSender _emailSender = emailSender;
        private readonly ISettingsService _settingsService = settingsService;

        public async Task<NotificationDto> SendNotificationAsync(CreateNotificationRequest request)
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.SenderEmail))
            {
                throw new ArgumentNullException("SenderEmail", "SenderEmail cannot be empty when sending notification.");
            }

            var notification = new Notification(0, request.Subject, request.Message, DateTime.UtcNow);
            await _emailSender.SendAsync(notification, settings, settings.UserEmail);
            return notification.ToDto();
        }
    }
}
