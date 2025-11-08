using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GamersCommunity.Core.Models
{
    /// <summary>
    /// Represents the health status of a microservice, including
    /// its general operational state and optional database connectivity state.
    /// </summary>
    /// <remarks>
    /// This model is returned by each microservice in response to
    /// a distributed health-check request (for example, a <c>System/Health Check</c> message).
    /// It uses the <see cref="HealthStatus"/> enumeration to ensure consistency
    /// with the ASP.NET Core health-check infrastructure.
    /// </remarks>
    public class MicroserviceHealth
    {
        /// <summary>
        /// Gets or sets the overall operational status of the microservice.
        /// </summary>
        /// <value>
        /// One of the values from <see cref="HealthStatus"/>:
        /// <c>Healthy</c>, <c>Degraded</c>, or <c>Unhealthy</c>.
        /// </value>
        /// <example>HealthStatus.Healthy</example>
        public required HealthStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the health status of the database connection
        /// associated with this microservice, if applicable.
        /// </summary>
        /// <value>
        /// One of the values from <see cref="HealthStatus"/>:
        /// <c>Healthy</c>, <c>Degraded</c>, or <c>Unhealthy</c>.
        /// This value may be <see langword="null"/> if the service has no database.
        /// </value>
        /// <example>HealthStatus.Healthy</example>
        public HealthStatus? Db { get; set; }
    }
}
