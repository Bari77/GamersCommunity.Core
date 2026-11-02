using System.Diagnostics;

namespace GamersCommunity.Core.Telemetry;

/// <summary>
/// One business operation. Disposing it records the duration and ends the span.
/// </summary>
public sealed class OperationScope : IDisposable
{
    private readonly string _operation;
    private readonly Activity? _activity;
    private readonly long _started;
    private bool _failed;
    private bool _disposed;

    internal OperationScope(string operation, Activity? activity)
    {
        _operation = operation;
        _activity = activity;
        _started = Stopwatch.GetTimestamp();
    }

    /// <summary>Marks the operation failed. The duration is still recorded.</summary>
    public void Fail()
    {
        _failed = true;
        _activity?.SetStatus(ActivityStatusCode.Error);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GamersTelemetry.Record(_operation, Stopwatch.GetElapsedTime(_started).TotalMilliseconds, _failed);
        _activity?.Dispose();
    }
}
