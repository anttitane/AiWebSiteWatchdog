using System;
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
    /// In-memory SSE broadcaster. Each subscriber gets its own channel ensuring
    /// all connected clients receive every published event (fan-out semantics).
    /// </summary>
    public sealed class SseEventPublisher
    {
        private readonly ConcurrentDictionary<Guid, Channel<SseEvent>> _subscribers = new();

        /// <summary>
        /// Subscribe to events. Returns (id, reader). Call Unsubscribe(id) when finished.
        /// </summary>
        public (Guid id, ChannelReader<SseEvent> reader) Subscribe()
        {
            var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            var id = Guid.NewGuid();
            _subscribers[id] = channel;
            return (id, channel.Reader);
        }

        public void Unsubscribe(Guid id)
        {
            if (_subscribers.TryRemove(id, out var ch))
            {
                try { ch.Writer.TryComplete(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Broadcast an event to all active subscribers. Removes dead subscribers.
        /// </summary>
        public void Publish(string name, object payload)
        {
            var evt = new SseEvent(name, payload);
            foreach (var kvp in _subscribers)
            {
                var writer = kvp.Value.Writer;
                // Attempt non-blocking write; if channel closed remove subscriber.
                if (!writer.TryWrite(evt))
                {
                    if (writer.TryComplete())
                    {
                        _subscribers.TryRemove(kvp.Key, out _);
                    }
                }
            }
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
