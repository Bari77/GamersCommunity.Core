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

    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

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
}
