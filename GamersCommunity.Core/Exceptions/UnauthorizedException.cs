using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception indicating that authentication is required or has failed (HTTP 401 - Unauthorized).
    /// </summary>
    /// <remarks>
    /// Use this when the request lacks valid authentication credentials or a provided token is invalid/expired.
    /// It maps to <see cref="HttpStatusCode.Unauthorized"/>. For authorization/permission errors, prefer
    /// <c>ForbiddenException</c> (HTTP 403).
    /// </remarks>
    /// <param name="code">Human-readable code of the validation or input error.</param>
    /// <param name="message">Human-readable description of the authentication failure.</param>
    /// <example>
    /// <code>
    /// if (!authService.IsTokenValid(token))
    ///     throw new UnauthorizedException("UNAUTHORIZED", "Invalid or expired access token.");
    /// </code>
    /// </example>
    public class UnauthorizedException(string code = "UNAUTHORIZED", string? message = "Authentication is required to access the resource.")
        : AppException(HttpStatusCode.Unauthorized, code, message)
    {
    }
}
