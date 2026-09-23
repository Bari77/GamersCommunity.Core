using Microsoft.Extensions.Options;
using Serilog;

namespace GamersCommunity.Core.Rabbit;

/// <summary>
/// Backward-compatible alias for <see cref="RabbitRpcClient"/> (DevKit / older hosts).
/// Prefer injecting <see cref="IRabbitRpcClient"/> in new code.
/// </summary>
public class RabbitMQProducer : IRabbitRpcClient, IAsyncDisposable
{
    private readonly RabbitRpcClient _inner;

    public RabbitMQProducer(IOptions<RabbitMQSettings> opts, ILogger logger)
        : this(opts, logger, options: null)
    {
    }

    public RabbitMQProducer(
        IOptions<RabbitMQSettings> opts,
        ILogger logger,
        RabbitRpcClientOptions? options)
    {
        _inner = new RabbitRpcClient(opts, logger, options);
    }

    public Task<string> CallAsync(string queue, string message, CancellationToken ct = default)
        => _inner.CallAsync(queue, message, ct);

    public Task<bool> HasActiveConsumerAsync(string queue, CancellationToken ct = default)
        => _inner.HasActiveConsumerAsync(queue, ct);

    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}
