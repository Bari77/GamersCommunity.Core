using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception representing a client-side input/validation error (HTTP 400 - Bad Request).
    /// </summary>
    /// <remarks>
    /// Use this exception to signal that the caller provided an invalid payload, parameters,
    /// or otherwise failed preconditions. It maps to <see cref="HttpStatusCode.BadRequest"/>.
    /// Handlers can catch <see cref="IAppException"/> to convert it into a standardized error response.
    /// </remarks>
    /// <param name="code">Human-readable code of the validation or input error.</param>
    /// <param name="message">Human-readable description of the validation or input error.</param>
    /// <example>
    /// <code>
    /// if (string.IsNullOrWhiteSpace(dto.Email))
    ///     throw new BadRequestException("Email is required.");
    /// </code>
    /// </example>
    public class BadRequestException(string code, string? message) : Exception(message), IAppException
    {
        /// <inheritdoc/>
        public string Code => code;

        /// <inheritdoc/>
        public HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
    }
}
