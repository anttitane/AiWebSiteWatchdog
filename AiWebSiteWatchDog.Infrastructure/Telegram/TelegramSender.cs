using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace AiWebSiteWatchDog.Infrastructure.Telegram
{
    public class TelegramSender : ITelegramSender
    {
        private const int MaxTelegramLength = 4096; // official limit
        private const int ChunkSoftLimit = 3900; // buffer to avoid edge cases after escaping

        private static readonly char[] MarkdownV2Chars = new[] {'\\','_','*','[',']','(',')','~','`','>','#','+','-','=','|','{','}','.','!'};

        private static string EscapeMarkdownV2(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new System.Text.StringBuilder(input.Length * 2);
            foreach (var c in input)
            {
                if (Array.IndexOf(MarkdownV2Chars, c) >= 0)
                {
                    sb.Append('\\').Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static System.Collections.Generic.IEnumerable<string> ChunkEscaped(string escaped)
        {
            if (escaped.Length <= ChunkSoftLimit)
            {
                yield return escaped;
                yield break;
            }
            var lines = escaped.Split('\n');
            var current = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                // +1 for the newline we will re-add (except possibly last)
                var toAdd = line;
                if (current.Length + toAdd.Length + 1 > ChunkSoftLimit)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }
                    if (toAdd.Length + 1 > ChunkSoftLimit)
                    {
                        // Very long single line: hard split at MaxTelegramLength boundaries
                        int idx = 0;
                        while (idx < toAdd.Length)
                        {
                            var take = Math.Min(ChunkSoftLimit, toAdd.Length - idx);
                            yield return toAdd.Substring(idx, take);
                            idx += take;
                        }
                        continue;
                    }
                }
                current.Append(toAdd).Append('\n');
            }
            if (current.Length > 0)
            {
                // Trim trailing newline
                if (current[current.Length - 1] == '\n') current.Length -= 1;
                yield return current.ToString();
            }
        }

        private static readonly ConcurrentDictionary<string, TelegramBotClient> _clientCache = new();

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
                var client = _clientCache.GetOrAdd(botToken, t => new TelegramBotClient(t));
                var raw = $"{notification.Subject}\n\n{notification.Message}".Trim();
                var escaped = EscapeMarkdownV2(raw);
                var chunks = ChunkEscaped(escaped);
                int index = 0;
                foreach (var chunk in chunks)
                {
                    var toSend = chunk;
                    if (index > 0)
                    {
                        // Indicate continuation for multi-part messages
                        toSend = EscapeMarkdownV2("(cont.)\n") + toSend;
                    }
                    Log.Information("Sending Telegram message chunk {ChunkIndex} to chat {ChatId} (len={Length})", index, chatId, toSend.Length);
                    try
                    {
                        await client.SendTextMessageAsync(chatId, toSend, parseMode: ParseMode.MarkdownV2, disableNotification: false);
                    }
                    catch (global::Telegram.Bot.Exceptions.ApiRequestException apiEx) when (apiEx.Message.Contains("can't parse entities", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning(apiEx, "Telegram MarkdownV2 parse error; retrying chunk {ChunkIndex} as plain text.", index);
                        await client.SendTextMessageAsync(chatId, chunk, disableNotification: false);
                    }
                    index++;
                }
                Log.Information("Telegram message sent to chat {ChatId} in {ChunkCount} chunk(s)", chatId, index);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send Telegram notification to chat {ChatId}", chatId);
                throw;
            }
        }
    }
}
