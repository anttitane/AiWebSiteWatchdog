using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using System;
using AiWebSiteWatchDog.Domain.DTOs;
using Microsoft.Extensions.Logging;

namespace AiWebSiteWatchDog.Application.Services
{
    public class NotificationService(ILogger<NotificationService> logger,
                                     IEmailSender emailSender,
                                     ITelegramSender telegramSender,
                                     ISettingsService settingsService,
                                     INotificationRepository notificationRepository) : INotificationService
    {
        private readonly ILogger<NotificationService> _logger = logger;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly ITelegramSender _telegramSender = telegramSender;
        private readonly ISettingsService _settingsService = settingsService;
        private readonly INotificationRepository _notificationRepository = notificationRepository;

        public async Task<NotificationDto> SendNotificationAsync(CreateNotificationRequest request)
        {
            var settings = await _settingsService.GetSettingsAsync() ?? throw new InvalidOperationException("User settings not configured.");
            var notification = new Notification(0, request.Subject, request.Message, DateTime.UtcNow);

            _logger.LogInformation("Dispatching notification via {Channel}. Subject: {Subject}", settings.NotificationChannel, request.Subject);

            try
            {
                switch (settings.NotificationChannel)
                {
                    case NotificationChannel.Email:
                        ValidateEmailSettings(settings);
                        await _emailSender.SendAsync(notification, settings, settings.UserEmail);
                        break;
                    case NotificationChannel.Telegram:
                        ValidateTelegramSettings(settings);
                        await _telegramSender.SendAsync(notification, settings, settings.TelegramChatId);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported notification channel selected.");
                }

                await _notificationRepository.AddAsync(notification);
                _logger.LogInformation("Notification persisted. Channel {Channel}. Subject: {Subject}", settings.NotificationChannel, request.Subject);
                return notification.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification via {Channel}. Subject: {Subject}", settings.NotificationChannel, request.Subject);
                throw;
            }
        }

        private static void ValidateEmailSettings(UserSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SenderEmail))
                throw new InvalidOperationException("SenderEmail cannot be empty when using Email notifications.");
            if (string.IsNullOrWhiteSpace(settings.UserEmail))
                throw new InvalidOperationException("UserEmail cannot be empty when using Email notifications.");
        }

        private static void ValidateTelegramSettings(UserSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.TelegramBotToken) || string.IsNullOrWhiteSpace(settings.TelegramChatId))
                throw new InvalidOperationException("Telegram configuration missing BotToken or ChatId.");
        }
    }
}
