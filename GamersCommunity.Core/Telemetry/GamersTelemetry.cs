using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace GamersCommunity.Core.Telemetry;

/// <summary>
/// Business spans and operation metrics for every GamersCommunity host.
/// Transport spans stay on <see cref="BusTelemetry"/>.
/// </summary>
/// <remarks>
/// A command span is <c>Users.Touch</c>. An event span is <c>user.identity.changed</c>.
/// The Rabbit span stays the queue name, so a trace reads business, then transport, then business.
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
    /// <c>Users.Create</c> when both parts are a short token. An all-caps action such as <c>CREATE</c> or <c>ADD_GUILD_MEMBER</c> is written in Pascal case.
    /// </summary>
    public static string OperationName(string? resource, string? action)
    {
        var left = Pascal(Token(resource));
        if (left is null)
            return "unknown";

        var right = Pascal(Token(action));
        return right is null ? left : $"{left}.{right}";
    }

    /// <summary>
    /// Keeps a lowercase dotted event name such as <c>user.identity.changed</c>, or <c>unknown</c>.
    /// </summary>
    public static string EventName(string? type)
    {
        var token = Token(type);
        if (token is null || !token.Contains('.'))
            return "unknown";

        return token.ToLowerInvariant();
    }

    private static string? Pascal(string? value)
    {
        if (value is null)
            return null;

        if (value.Any(char.IsLower))
            return value;

        var parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var builder = new StringBuilder(value.Length);
        foreach (var part in parts)
        {
            builder.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
                builder.Append(part[1..].ToLowerInvariant());
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

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
