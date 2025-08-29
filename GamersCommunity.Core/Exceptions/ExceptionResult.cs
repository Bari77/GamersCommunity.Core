namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Standard error payload returned to clients when an exception is handled by the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This DTO is designed to be safe for client consumption. The <see cref="Message"/> is a
    /// human-readable summary. The <see cref="Exception"/> field can optionally contain
    /// developer-friendly details (stack trace, exception type) and should typically be included
    /// only in development environments. The <see cref="TraceId"/> helps correlate logs and requests.
    /// </para>
    /// <para>
    /// API layers can serialize this type to JSON for consistent error responses.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example JSON response:
    /// <code>
    /// {
    ///   "message": "An unexpected error occurred.",
    ///   "exception": "System.NullReferenceException: ...",
    ///   "traceId": "00-5b8c9e2f7b9d3b91c2f7e3d9f1ab1234-1c2b3a4d5e6f7890-00"
    /// }
    /// </code>
    /// </example>
    public class ExceptionResult
    {
        /// <summary>
        /// Human-readable error message intended for clients.
        /// </summary>
        /// <remarks>
        /// Prefer concise, user-safe wording. Avoid leaking sensitive details.
        /// Defaults to a generic message.
        /// </remarks>
        public string Message { get; set; } = "An unexpected error occurred.";

        /// <summary>
        /// Optional developer-oriented exception details (e.g., type, stack trace).
        /// </summary>
        /// <remarks>
        /// Include only in non-production environments or behind a feature flag to avoid exposing
        /// sensitive information to end users.
        /// </remarks>
        public string? Exception { get; set; }

        /// <summary>
        /// Optional trace or correlation identifier associated with the request/operation.
        /// </summary>
        /// <remarks>
        /// Use this value to correlate client-visible errors with server-side logs and telemetry.
        /// Commonly populated from an HTTP trace id or a logging scope property.
        /// </remarks>
        public string? TraceId { get; set; }
    }
}
