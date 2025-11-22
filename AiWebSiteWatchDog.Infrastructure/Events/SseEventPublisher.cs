using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Text.Json;

namespace AiWebSiteWatchDog.Infrastructure.Events
{
    public sealed class SseEvent
    {
        public string Name { get; }
        public object Data { get; }
        public SseEvent(string name, object data)
        {
            Name = name;
            Data = data;
        }
    }

    /// <summary>
    /// Simple in-memory broadcaster for server-sent events. Single reader, multi-writer.
    /// </summary>
    public sealed class SseEventPublisher
    {
        private readonly Channel<SseEvent> _channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        public ChannelReader<SseEvent> Reader => _channel.Reader;

        public void Publish(string name, object payload)
        {
            _channel.Writer.TryWrite(new SseEvent(name, payload));
        }

        public string Serialize(object payload)
        {
            return JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
