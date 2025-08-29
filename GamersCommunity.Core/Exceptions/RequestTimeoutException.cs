using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception indicating that the client did not produce a request within the time the server was prepared to wait (HTTP 408 - Request Timeout).
    /// </summary>
    /// <remarks>
    /// Use this for client-originated timeouts (e.g., slow upload, body not received in time, client stalled),
    /// as opposed to upstream/service timeouts which should map to <c>GatewayTimeoutException</c> (HTTP 504).
    /// Handlers can catch <see cref="IAppException"/> to convert it into a standardized error response.
    /// </remarks>
    /// <param name="message">Human-readable description of the timeout condition.</param>
    /// <example>
    /// <code>
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    /// if (!await reader.ReadAsync(cts.Token))
    ///     throw new RequestTimeoutException("Request body was not received within 30 seconds.");
    /// </code>
    /// </example>
    public class RequestTimeoutException(string message) : Exception(message), IAppException
    {
        /// <summary>
        /// Gets the HTTP status code associated with this exception (408).
        /// </summary>
        public HttpStatusCode Code => HttpStatusCode.RequestTimeout;
    }
}
