namespace GamersCommunity.Core.Rabbit;

/// <summary>
/// RPC-style client: publish to a queue and wait for a correlated reply envelope.
/// </summary>
public interface IRabbitRpcClient
{
    /// <summary>
    /// Sends an RPC message and returns the string payload from a successful <see cref="RpcEnvelope{T}"/>.
    /// </summary>
    Task<string> CallAsync(string queue, string payload, CancellationToken ct = default);

    /// <summary>
    /// Returns whether at least one consumer is listening on <paramref name="queue"/>.
    /// A missing queue is treated as no consumer.
    /// </summary>
    Task<bool> HasActiveConsumerAsync(string queue, CancellationToken ct = default);
}
