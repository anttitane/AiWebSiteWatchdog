using System;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace AiWebSiteWatchDog.Infrastructure.Telegram
{
    public class TelegramSender : ITelegramSender
    {
        public async Task SendAsync(Notification notification, UserSettings settings, string? chatIdOverride = null)
        {
            if (settings.NotificationChannel != NotificationChannel.Telegram)
            {
                Log.Warning("TelegramSender invoked while channel is {Channel}", settings.NotificationChannel);
                return; // No-op if not selected
            }
            var botToken = settings.TelegramBotToken;
            var chatId = chatIdOverride ?? settings.TelegramChatId;
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                throw new InvalidOperationException("Telegram bot token or chat ID not configured.");

            try
            {
                var client = new TelegramBotClient(botToken);
                var text = $"{notification.Subject}\n\n{notification.Message}";
                Log.Information("Sending Telegram message to chat {ChatId}", chatId);
                await client.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, disableNotification: false);
                Log.Information("Telegram message sent to chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send Telegram notification to chat {ChatId}", chatId);
                throw;
            }
        }
    }
}
