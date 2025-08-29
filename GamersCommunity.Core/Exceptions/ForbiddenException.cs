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
    /// <param name="message">Human-readable explanation of the authorization/permission failure.</param>
    /// <example>
    /// <code>
    /// if (!user.HasPermission(Permissions.DeletePost))
    ///     throw new ForbiddenException("You are not allowed to delete this post.");
    /// </code>
    /// </example>
    public class ForbiddenException(string message) : Exception(message), IAppException
    {
        /// <summary>
        /// Gets the HTTP status code associated with this exception (403).
        /// </summary>
        public HttpStatusCode Code => HttpStatusCode.Forbidden;
    }
}
