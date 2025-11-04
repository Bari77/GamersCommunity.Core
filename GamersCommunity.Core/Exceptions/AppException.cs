using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Represents a standardized application-level exception that includes an HTTP status code and an error code.
    /// </summary>
    /// <remarks>
    /// This exception is typically thrown to indicate known, controlled errors within the application
    /// that should be converted into structured HTTP responses by the API layer.
    /// It provides both a <see cref="StatusCode"/> and a <see cref="Code"/> to make error handling consistent.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// throw new AppException(HttpStatusCode.BadRequest, "INVALID_INPUT", "The provided data is invalid.");
    /// </code>
    /// </example>
    /// <param name="statusCode">The HTTP status code that represents the nature of the error (e.g., 400, 404, 500).</param>
    /// <param name="code">A short, machine-readable code uniquely identifying the error type.</param>
    /// <param name="message">An optional human-readable error message describing the issue.</param>
    public class AppException(HttpStatusCode statusCode, string code, string? message) : Exception(message), IAppException
    {
        /// <inheritdoc/>
        public string Code => code;

        /// <inheritdoc/>
        public HttpStatusCode StatusCode => statusCode;
    }
}
