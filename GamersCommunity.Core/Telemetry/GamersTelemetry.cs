using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GamersCommunity.Core.Telemetry;

/// <summary>
/// Business spans and operation metrics for every GamersCommunity host.
/// Transport spans stay on <see cref="BusTelemetry"/>.
/// </summary>
/// <remarks>
/// Tag an operation with a closed name (<c>Users.Update</c>, <c>user.identity.changed</c>).
/// Do not tag user ids, emails, tokens, or payloads: those series never stay bounded.
/// </remarks>
public static class GamersTelemetry
{
    /// <summary>
    /// <see cref="ActivitySource"/> name registered by <c>AddGamersCommunityTelemetry</c>.
    /// </summary>
    public const string SourceName = "GamersCommunity";

    /// <summary>
    /// <see cref="Meter"/> name registered by <c>AddGamersCommunityTelemetry</c>.
    /// </summary>
    public const string MeterName = "GamersCommunity";

    /// <summary>Histogram that carries operation duration, in milliseconds.</summary>
    public const string DurationName = "gc.operation.duration";

    private static readonly ActivitySource Source = new(SourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        DurationName,
        unit: "ms",
        description: "Business operation duration.");
    private static readonly Counter<long> Errors = Meter.CreateCounter<long>(
        "gc.operation.errors",
        description: "Business operations that failed.");

    /// <summary>
    /// Starts a span named <c>Resource.Action</c> and records its duration when disposed.
    /// </summary>
    /// <param name="resource">Registered service resource.</param>
    /// <param name="action">Bus action.</param>
    public static OperationScope StartOperation(string? resource, string? action) =>
        StartOperation(OperationName(resource, action));

    /// <summary>
    /// Starts a span for an integration event type and records its duration when disposed.
    /// The span carries <c>event.type</c> as well as <c>operation</c>.
    /// </summary>
    /// <param name="type">Event discriminator.</param>
    public static OperationScope StartEvent(string? type)
    {
        var name = EventName(type);
        var activity = Source.StartActivity(name, ActivityKind.Internal);
        activity?.SetTag("operation", name);
        activity?.SetTag("event.type", name);
        return new OperationScope(name, activity);
    }

    /// <summary>
    /// Starts a nested business span. It becomes a child of the current span.
    /// </summary>
    /// <param name="name">Closed operation name, already normalized.</param>
    public static OperationScope StartOperation(string name)
    {
        var operation = string.IsNullOrWhiteSpace(name) ? "unknown" : name;
        var activity = Source.StartActivity(operation, ActivityKind.Internal);
        activity?.SetTag("operation", operation);
        return new OperationScope(operation, activity);
    }

    /// <summary>
    /// <c>Users.Update</c> when both parts are a short token, otherwise <c>unknown</c> or the resource alone.
    /// </summary>
    public static string OperationName(string? resource, string? action)
    {
        var left = Token(resource);
        if (left is null)
            return "unknown";

        var right = Token(action);
        return right is null ? left : $"{left}.{right}";
    }

    /// <summary>
    /// Keeps a short event discriminator, or <c>unknown</c> when it is not a token.
    /// </summary>
    public static string EventName(string? type) => Token(type) ?? "unknown";

    internal static void Record(string operation, double elapsedMs, bool failed)
    {
        Duration.Record(elapsedMs, new TagList
        {
            { "operation", operation },
            { "result", failed ? "error" : "ok" },
        });
        if (failed)
            Errors.Add(1, new TagList { { "operation", operation } });
    }

    private static string? Token(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();
        if (text.Length is < 1 or > 80)
            return null;

        foreach (var character in text)
        {
            var allowed = character is (>= 'A' and <= 'Z')
                or (>= 'a' and <= 'z')
                or (>= '0' and <= '9')
                or '_'
                or '.';
            if (!allowed)
                return null;
        }

        return text;
    }
}
