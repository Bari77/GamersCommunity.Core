using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception indicating that an upstream dependency failed to respond in time (HTTP 504 - Gateway Timeout).
    /// </summary>
    /// <remarks>
    /// Use this when acting as a gateway/proxy (e.g., RPC over RabbitMQ, HTTP to another service)
    /// and no response is received within the configured timeout window.
    /// It maps to <see cref="HttpStatusCode.GatewayTimeout"/>.
    /// Handlers can catch <see cref="IAppException"/> to return a standardized error payload.
    /// </remarks>
    /// <param name="code">Human-readable code of the validation or input error.</param>
    /// <param name="message">Human-readable description of the timeout condition.</param>
    /// <example>
    /// <code>
    /// // After waiting for an RPC response beyond the allowed duration:
    /// throw new GatewayTimeoutException($"No response from worker within {timeoutSeconds}s.");
    /// </code>
    /// </example>
    public class GatewayTimeoutException(string code, string? message) : Exception(message), IAppException
    {
        /// <inheritdoc/>
        public string Code => code;

        /// <inheritdoc/>
        public HttpStatusCode StatusCode => HttpStatusCode.GatewayTimeout;
    }
}
