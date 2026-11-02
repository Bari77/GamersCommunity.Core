using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace GamersCommunity.Core.Telemetry;

/// <summary>
/// RabbitMQ trace context and bus metrics shared by every host.
/// </summary>
public static class BusTelemetry
{
    /// <summary>
    /// <see cref="ActivitySource"/> name registered by <c>AddGamersCommunityTelemetry</c>.
    /// </summary>
    public const string SourceName = "GamersCommunity.Bus";

    /// <summary>
    /// <see cref="Meter"/> name registered by <c>AddGamersCommunityTelemetry</c>.
    /// </summary>
    public const string MeterName = "GamersCommunity.Bus";

    private static readonly ActivitySource Source = new(SourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> RpcDuration = Meter.CreateHistogram<double>(
        "gc.bus.rpc.duration",
        unit: "ms",
        description: "Rabbit RPC round-trip duration.");
    private static readonly Counter<long> RpcErrors = Meter.CreateCounter<long>(
        "gc.bus.rpc.errors",
        description: "Rabbit RPC calls that failed.");
    /// <summary>Histogram that carries consumer handling time, in milliseconds.</summary>
    internal const string ProcessDurationName = "gc.bus.process.duration";

    /// <summary>Gauge of messages currently inside a consumer, one series per queue.</summary>
    internal const string InflightName = "gc.bus.inflight";

    private static readonly Counter<long> Messages = Meter.CreateCounter<long>(
        "gc.bus.messages",
        description: "RabbitMQ messages handled by a consumer.");
    private static readonly Histogram<double> ProcessDuration = Meter.CreateHistogram<double>(
        ProcessDurationName,
        unit: "ms",
        description: "Time spent handling one RabbitMQ message.");
    private static readonly ConcurrentDictionary<string, int> InflightByQueue = new();

    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    static BusTelemetry()
    {
        Meter.CreateObservableGauge(
            InflightName,
            ObserveInflight,
            description: "RabbitMQ messages currently being handled. Labeled only by queue.");
    }

    /// <summary>
    /// Starts a client span for an outbound Rabbit publish and makes it current.
    /// </summary>
    /// <param name="name">Span name.</param>
    /// <param name="destination">Queue or exchange name.</param>
    public static Activity? StartClient(string name, string destination) =>
        Start(name, ActivityKind.Client, destination, parent: default);

    /// <summary>
    /// Starts a producer span for a fire-and-forget publish.
    /// </summary>
    /// <param name="name">Span name.</param>
    /// <param name="destination">Queue or exchange name.</param>
    public static Activity? StartProducer(string name, string destination) =>
        Start(name, ActivityKind.Producer, destination, parent: default);

    /// <summary>
    /// Starts a consumer span parented by the W3C context carried in <paramref name="headers"/>.
    /// Disposing the result restores the previous activity and baggage.
    /// </summary>
    /// <param name="name">Span name.</param>
    /// <param name="headers">Inbound AMQP headers. May be <c>null</c>.</param>
    /// <param name="destination">Queue name.</param>
    public static BusTraceScope StartConsumer(string name, IDictionary<string, object?>? headers, string? destination)
    {
        var parent = Propagator.Extract(default, headers, GetHeaderValues);
        var activity = Start(name, ActivityKind.Consumer, destination, parent.ActivityContext);
        return new BusTraceScope(activity, Baggage.Current, parent.Baggage);
    }

    /// <summary>
    /// Writes the current W3C trace context into <paramref name="headers"/>.
    /// </summary>
    /// <param name="headers">Outbound AMQP headers.</param>
    public static void Inject(IDictionary<string, object?> headers)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        Propagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            headers,
            static (carrier, key, value) => carrier[key] = value);
    }

    /// <summary>
    /// Counts one consumed message. The destination is the queue name, which stays a closed set.
    /// </summary>
    /// <param name="destination">Queue name.</param>
    /// <param name="failed">Whether handling the message failed.</param>
    public static void RecordMessage(string? destination, bool failed) =>
        RecordHandled(destination, failed, elapsedMs: null, eventType: null);

    /// <summary>
    /// Counts one consumed message and records how long it took.
    /// </summary>
    /// <param name="destination">Queue name. Stored as <c>messaging.destination.name</c>.</param>
    /// <param name="failed">Whether handling the message failed.</param>
    /// <param name="elapsedMs">Handling time in milliseconds.</param>
    /// <param name="eventType">
    /// Integration event name such as <c>user.identity.changed</c>.
    /// Omitted for a command, and never a user id.
    /// </param>
    internal static void RecordHandled(string? destination, bool failed, double elapsedMs, string? eventType) =>
        RecordHandled(destination, failed, (double?)elapsedMs, eventType);

    /// <summary>
    /// Marks one message as in flight until the returned handle is disposed.
    /// </summary>
    /// <param name="destination">Queue name.</param>
    /// <returns>A handle that decrements <see cref="InflightName"/>.</returns>
    internal static IDisposable Enter(string? destination)
    {
        var queue = Queue(destination);
        InflightByQueue.AddOrUpdate(queue, 1, static (_, count) => count + 1);
        return new InflightLease(queue);
    }

    /// <summary>
    /// Records one RPC round-trip.
    /// </summary>
    /// <param name="queue">Destination queue.</param>
    /// <param name="elapsedMs">Elapsed milliseconds.</param>
    /// <param name="failed">Whether the call threw or returned an RPC error.</param>
    public static void RecordRpc(string queue, double elapsedMs, bool failed)
    {
        var tags = new TagList { { "messaging.destination.name", queue } };
        RpcDuration.Record(elapsedMs, tags);
        if (failed)
            RpcErrors.Add(1, tags);
    }

    private static void RecordHandled(string? destination, bool failed, double? elapsedMs, string? eventType)
    {
        var tags = new TagList
        {
            { "messaging.destination.name", Queue(destination) },
            { "result", failed ? "error" : "ok" },
        };
        var eventName = GamersTelemetry.EventName(eventType);
        if (eventName != "unknown")
            tags.Add("event", eventName);

        Messages.Add(1, tags);
        if (elapsedMs is double elapsed)
            ProcessDuration.Record(elapsed, tags);
    }

    private static IEnumerable<Measurement<int>> ObserveInflight()
    {
        foreach (var pair in InflightByQueue)
            yield return new Measurement<int>(pair.Value, new TagList { { "messaging.destination.name", pair.Key } });
    }

    private static string Queue(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return "unknown";

        var text = destination.Trim();
        if (text.Length is < 1 or > 80)
            return "unknown";

        foreach (var character in text)
        {
            var allowed = character is (>= 'A' and <= 'Z')
                or (>= 'a' and <= 'z')
                or (>= '0' and <= '9')
                or '_'
                or '.'
                or '-';
            if (!allowed)
                return "unknown";
        }

        return text;
    }

    private static Activity? Start(string name, ActivityKind kind, string? destination, ActivityContext parent)
    {
        var activity = kind == ActivityKind.Consumer
            ? Source.StartActivity(name, kind, parent)
            : Source.StartActivity(name, kind);

        if (activity is null)
            return null;

        activity.SetTag("messaging.system", "rabbitmq");
        if (!string.IsNullOrWhiteSpace(destination))
            activity.SetTag("messaging.destination.name", destination);

        return activity;
    }

    private static IEnumerable<string> GetHeaderValues(IDictionary<string, object?>? carrier, string key)
    {
        if (carrier is null || !TryGet(carrier, key, out var value) || value is null)
            yield break;

        var text = value switch
        {
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            _ => value.ToString(),
        };

        if (!string.IsNullOrEmpty(text))
            yield return text;
    }

    private static bool TryGet(IDictionary<string, object?> carrier, string key, out object? value)
    {
        if (carrier.TryGetValue(key, out value))
            return true;

        foreach (var pair in carrier)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private sealed class InflightLease(string queue) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            InflightByQueue.AddOrUpdate(queue, 0, static (_, count) => count > 0 ? count - 1 : 0);
        }
    }
}
