namespace GamersCommunity.Core.Rabbit;

/// <summary>
/// Behaviour toggles for <see cref="RabbitRpcClient"/>.
/// </summary>
public sealed class RabbitRpcClientOptions
{
    /// <summary>
    /// When true, <see cref="IRabbitRpcClient.CallAsync"/> fails fast if the target queue has no consumer
    /// (Gateway edge behaviour). Game-to-Platform RPCs leave this false.
    /// </summary>
    public bool RequireActiveConsumer { get; init; }
}
