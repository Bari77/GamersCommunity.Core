﻿using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    /// <summary>
    /// Exception indicating that a requested resource could not be found (HTTP 404 - Not Found).
    /// </summary>
    /// <remarks>
    /// Use this when an entity, record, or endpoint-specific resource is absent.
    /// It maps to <see cref="HttpStatusCode.NotFound"/> and can be handled uniformly via <see cref="IAppException"/>.
    /// </remarks>
    /// <param name="message">Human-readable description of the missing resource.</param>
    /// <example>
    /// <code>
    /// var user = await db.Users.FindAsync(id, ct);
    /// if (user is null)
    ///     throw new NotFoundException($"User with id {id} was not found.");
    /// </code>
    /// </example>
    public class NotFoundException(string message) : Exception(message), IAppException
    {
        /// <summary>
        /// Gets the HTTP status code associated with this exception (404).
        /// </summary>
        public HttpStatusCode Code => HttpStatusCode.NotFound;
    }
}
