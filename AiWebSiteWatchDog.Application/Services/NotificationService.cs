using AiWebSiteWatchDog.Application.Services;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailSender _emailSender;
        private readonly ISettingsService _settingsService;

        public NotificationService(IEmailSender emailSender, ISettingsService settingsService)
        {
            _emailSender = emailSender;
            _settingsService = settingsService;
        }

        public async Task SendNotificationAsync(Notification notification)
        {
            var settings = await _settingsService.GetSettingsAsync();
            await _emailSender.SendAsync(notification, settings.EmailSettings);
        }
    }
}
