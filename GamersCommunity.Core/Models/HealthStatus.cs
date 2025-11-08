namespace GamersCommunity.Core.Models
{
    /// <summary>
    /// Represents the health status of a microservice, including
    /// its general operational state and database connectivity.
    /// </summary>
    public class HealthStatus
    {
        /// <summary>
        /// Gets or sets the name of the microservice reporting its health status.
        /// </summary>
        /// <example>Users</example>
        public required string Service { get; set; }

        /// <summary>
        /// Gets or sets the overall operational status of the microservice.
        /// Typical values are <c>"Healthy"</c>, <c>"Degraded"</c>, or <c>"Unhealthy"</c>.
        /// </summary>
        /// <example>Healthy</example>
        public required string Status { get; set; }

        /// <summary>
        /// Gets or sets the health status of the database connection
        /// for this microservice, including potential error messages if any.
        /// </summary>
        /// <example>Healthy</example>
        public required string Db { get; set; }
    }
}
