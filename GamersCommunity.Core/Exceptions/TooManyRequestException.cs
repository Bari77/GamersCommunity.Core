using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception indicating that the client has sent too many requests in a given amount of time (HTTP 429 - Too Many Requests).
    /// </summary>
    /// <remarks>
    /// Use this to signal rate limiting or quota enforcement. When returning this error from an HTTP endpoint,
    /// consider including a <c>Retry-After</c> header to advise the client when it may retry safely.
    /// Handlers can catch <see cref="IAppException"/> to produce a standardized error response.
    /// </remarks>
    /// <param name="message">Human-readable explanation of the throttling/quota violation.</param>
    /// <example>
    /// <code>
    /// if (!rateLimiter.AllowRequest(userId))
    /// {
    ///     throw new TooManyRequestsException("Request rate limit exceeded. Please try again later.");
    /// }
    /// </code>
    /// </example>
    public class TooManyRequestsException(string message) : Exception(message), IAppException
    {
        /// <summary>
        /// Gets the HTTP status code associated with this exception (429).
        /// </summary>
        public HttpStatusCode Code => HttpStatusCode.TooManyRequests;
    }
}
