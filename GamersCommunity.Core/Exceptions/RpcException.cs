namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception thrown by the producer when the consumer returns an RPC envelope with <c>ok=false</c>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RpcException"/> class.
    /// </remarks>
    /// <param name="code">A short, machine-friendly error code.</param>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="details">Optional technical details.</param>
    public sealed class RpcException(string code, string message, string? details = null) : Exception($"{code}: {message}{(details is null ? "" : $" — {details}")}")
    {
        /// <summary>
        /// Gets the error code returned by the consumer.
        /// </summary>
        public string Code { get; } = code;

        /// <summary>
        /// Gets additional error details returned by the consumer, when available.
        /// </summary>
        public string? Details { get; } = details;
    }
}
