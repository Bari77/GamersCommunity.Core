namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Generic RPC envelope used to standardize responses between producer and consumer.
    /// Wraps a success flag, an optional payload, and an optional error object.
    /// </summary>
    /// <typeparam name="T">Type of the payload carried in the envelope.</typeparam>
    public sealed record RpcEnvelope<T>(bool Ok, T? Data, RpcError? Error);
}
