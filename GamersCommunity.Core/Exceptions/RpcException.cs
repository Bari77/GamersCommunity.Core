using System.Net;

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
    public sealed class RpcException(string code = "INTERNAL_SERVER_ERROR", string? message = "Internal RPC Server Error.", string? details = null)
        : AppException(HttpStatusCode.InternalServerError, code, $"{message}{(details is null ? "" : $" — {details}")}")
    {
        /// <summary>
        /// Gets additional error details returned by the consumer, when available.
        /// </summary>
        public string? Details { get; } = details;
    }
}
