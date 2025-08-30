namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Structured error information returned by an RPC call when <see cref="RpcEnvelope{T}.ok"/> is <c>false</c>.
    /// </summary>
    /// <param name="Code">A short, machine-friendly error code (e.g., "ROUTING_ERROR").</param>
    /// <param name="Message">A human-readable error message.</param>
    /// <param name="Details">Optional technical details (stack trace excerpt, driver message, etc.).</param>
    public sealed record RpcError(string Code, string Message, string? Details = null);
}
