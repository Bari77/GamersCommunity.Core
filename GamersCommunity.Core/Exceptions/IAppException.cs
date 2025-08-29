using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Marker interface for application-specific exceptions that map to an HTTP status code.
    /// </summary>
    /// <remarks>
    /// Use this interface on custom exceptions to allow middleware/handlers to detect them and
    /// translate them into consistent HTTP responses without coupling to concrete types.
    /// </remarks>
    public interface IAppException
    {
        /// <summary>
        /// Gets the HTTP status code that should be returned to the client for this exception.
        /// </summary>
        HttpStatusCode Code { get; }
    }
}
