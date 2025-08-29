using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception representing an unexpected server-side failure (HTTP 500 - Internal Server Error).
    /// </summary>
    /// <remarks>
    /// Use this for unrecoverable errors that are not the client's fault (e.g., unexpected nulls,
    /// failed dependencies without a more specific mapping, or invariant violations).
    /// It maps to <see cref="HttpStatusCode.InternalServerError"/>.
    /// Prefer not to expose sensitive details to clients; include specifics only in server logs.
    /// </remarks>
    /// <param name="message">Human-readable summary of the server error.</param>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     var result = await repository.SaveAsync(entity, ct);
    /// }
    /// catch (Exception ex)
    /// {
    ///     // Log the full exception internally, return a generic 500 to the client.
    ///     logger.LogError(ex, "Unexpected error while saving entity {Id}", entity.Id);
    ///     throw new InternalServerErrorException("An unexpected error occurred while processing your request.");
    /// }
    /// </code>
    /// </example>
    public class InternalServerErrorException(string message) : Exception(message), IAppException
    {
        /// <summary>
        /// Gets the HTTP status code associated with this exception (500).
        /// </summary>
        public HttpStatusCode Code => HttpStatusCode.InternalServerError;
    }
}
