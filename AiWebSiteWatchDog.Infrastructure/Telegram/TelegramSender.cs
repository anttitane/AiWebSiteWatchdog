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

        // Produce both escaped and original raw chunks so we can fall back to raw on parse errors
        private static System.Collections.Generic.IEnumerable<(string Escaped, string Raw)> ChunkRawWithEscaping(string raw)
        {
            // Fast path
            var escapedWhole = EscapeMarkdownV2(raw);
            if (escapedWhole.Length <= ChunkSoftLimit)
            {
                yield return (escapedWhole, raw);
                yield break;
            }

            var lines = raw.Split('\n');
            var escapedBuilder = new System.Text.StringBuilder();
            var rawBuilder = new System.Text.StringBuilder();
            foreach (var lineRaw in lines)
            {
                var lineEscaped = EscapeMarkdownV2(lineRaw);
                // +1 for newline we will append (except maybe last)
                if (escapedBuilder.Length + lineEscaped.Length + 1 > ChunkSoftLimit)
                {
                    if (escapedBuilder.Length > 0)
                    {
                        // finalize current chunk (trim possible trailing newline)
                        if (escapedBuilder.Length > 0 && escapedBuilder[escapedBuilder.Length - 1] == '\n') escapedBuilder.Length -= 1;
                        if (rawBuilder.Length > 0 && rawBuilder[rawBuilder.Length - 1] == '\n') rawBuilder.Length -= 1;
                        yield return (escapedBuilder.ToString(), rawBuilder.ToString());
                        escapedBuilder.Clear();
                        rawBuilder.Clear();
                    }
                    if (lineEscaped.Length + 1 > ChunkSoftLimit)
                    {
                        // Very long single raw line: hard split preserving raw and escaped mapping
                        int idx = 0;
                        while (idx < lineRaw.Length)
                        {
                            // Start with max remaining length in raw; shrink until escaped fits within limit
                            int take = Math.Min(ChunkSoftLimit, lineRaw.Length - idx);
                            string segmentRaw;
                            string segmentEscaped;
                            while (true)
                            {
                                segmentRaw = lineRaw.Substring(idx, take);
                                segmentEscaped = EscapeMarkdownV2(segmentRaw);
                                if (segmentEscaped.Length <= ChunkSoftLimit || take <= 1) break;
                                // reduce take size (heuristic halve when very large, else decrement)
                                take = take > 200 ? take - 100 : take - 1;
                            }
                            yield return (segmentEscaped, segmentRaw);
                            idx += segmentRaw.Length;
                        }
                        continue; // move to next line
                    }
                }
                escapedBuilder.Append(lineEscaped).Append('\n');
                rawBuilder.Append(lineRaw).Append('\n');
            }
            if (escapedBuilder.Length > 0)
            {
                if (escapedBuilder[escapedBuilder.Length - 1] == '\n') escapedBuilder.Length -= 1;
                if (rawBuilder.Length > 0 && rawBuilder[rawBuilder.Length - 1] == '\n') rawBuilder.Length -= 1;
                yield return (escapedBuilder.ToString(), rawBuilder.ToString());
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
                var rawAggregated = ($"{notification.Subject}\n\n{notification.Message}").Trim();
                int index = 0;
                var prefixRaw = "(cont.)\n";
                var prefixEscaped = EscapeMarkdownV2(prefixRaw);
                foreach (var (Escaped, Raw) in ChunkRawWithEscaping(rawAggregated))
                {
                    var toSendEscaped = index > 0 ? prefixEscaped + Escaped : Escaped;
                    var fallbackRaw = index > 0 ? prefixRaw + Raw : Raw;
                    Log.Information("Sending Telegram message chunk {ChunkIndex} to chat {ChatId} (lenEscaped={EscapedLength}, lenRaw={RawLength})", index, chatId, toSendEscaped.Length, fallbackRaw.Length);
                    try
                    {
                        await client.SendTextMessageAsync(chatId, toSendEscaped, parseMode: ParseMode.MarkdownV2, disableNotification: false);
                    }
                    catch (global::Telegram.Bot.Exceptions.ApiRequestException apiEx) when (apiEx.Message.Contains("can't parse entities", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning(apiEx, "Telegram MarkdownV2 parse error; retrying chunk {ChunkIndex} as plain text.", index);
                        await client.SendTextMessageAsync(chatId, fallbackRaw, disableNotification: false);
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
