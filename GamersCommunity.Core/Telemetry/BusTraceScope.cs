using System.Diagnostics;
using OpenTelemetry;

namespace GamersCommunity.Core.Telemetry;

/// <summary>
/// A consumer span plus the baggage that was current before it started.
/// </summary>
public readonly struct BusTraceScope : IDisposable
{
    private readonly Baggage _previousBaggage;

    /// <summary>
    /// Creates a scope. Prefer <see cref="BusTelemetry.StartConsumer"/>.
    /// </summary>
    /// <param name="activity">The span to dispose, if a listener is registered.</param>
    /// <param name="previousBaggage">Baggage to restore on <see cref="Dispose"/>.</param>
    /// <param name="baggage">Baggage extracted from the inbound message.</param>
    public BusTraceScope(Activity? activity, Baggage previousBaggage, Baggage baggage)
    {
        Activity = activity;
        _previousBaggage = previousBaggage;
        Baggage.Current = baggage;
    }

    /// <summary>
    /// The consumer span, or <c>null</c> when nothing is listening.
    /// </summary>
    public Activity? Activity { get; }

    /// <summary>
    /// Replaces the span name once the message has been parsed.
    /// </summary>
    /// <param name="name">New span name.</param>
    public void SetName(string name)
    {
        if (Activity is not null)
            Activity.DisplayName = name;
    }

    /// <summary>
    /// Ends the span and restores the previous baggage.
    /// </summary>
    public void Dispose()
    {
        Activity?.Dispose();
        Baggage.Current = _previousBaggage;
    }
}
