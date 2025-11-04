using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception representing a permission or authorization failure (HTTP 403 - Forbidden).
    /// </summary>
    /// <remarks>
    /// Use this when the caller is authenticated but not allowed to perform the requested action
    /// or access the requested resource. It maps to <see cref="HttpStatusCode.Forbidden"/>.
    /// Handlers can catch <see cref="IAppException"/> to produce standardized error responses.
    /// </remarks>
    /// <param name="code">Human-readable code of the validation or input error.</param>
    /// <param name="message">Human-readable explanation of the authorization/permission failure.</param>
    /// <example>
    /// <code>
    /// if (!user.HasPermission(Permissions.DeletePost))
    ///     throw new ForbiddenException("FORBIDDEN", "You are not allowed to delete this post.");
    /// </code>
    /// </example>
    public class ForbiddenException(string code = "FORBIDDEN", string? message = "The server understood the request but refuses to execute it.")
        : AppException(HttpStatusCode.Forbidden, code, message)
    {
    }
}
