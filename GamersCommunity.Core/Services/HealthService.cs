using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Models;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GamersCommunity.Core.Services
{
    /// <summary>
    /// Health check service
    /// </summary>
    /// <typeparam name="TContext">Db context to check</typeparam>
    /// <param name="dbContext">Db context to check</param>
    public class HealthService<TContext>(TContext dbContext) : IBusService
        where TContext : DbContext
    {
        /// <inheritdoc/>
        BusServiceTypeEnum IBusService.Type => BusServiceTypeEnum.INFRA;
        /// <inheritdoc/>
        public string Resource => "Health";

        /// <inheritdoc/>
        public async Task<string> HandleAsync(BusMessage message, CancellationToken ct = default)
        {
            switch (message.Action.ToUpperInvariant())
            {
                case "CHECK":
                    return JsonSafe.Serialize(await CheckAsync(ct));

                default:
                    Log.Warning($"Action {message.Action} not implemented");
                    throw new InternalServerErrorException("ACTION_NOT_IMPLEMENTED", $"Action {message.Action} not implemented");
            }
        }

        /// <summary>
        /// Persists a new entity and returns its generated identifier.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created entity identifier.</returns>
        protected async Task<HealthStatus> CheckAsync(CancellationToken ct = default)
        {
            try
            {
                var dbStatus = await dbContext.Database.CanConnectAsync(ct)
                    ? "Healthy"
                    : "Degraded";

                return new HealthStatus()
                {
                    Status = "Healthy",
                    Db = dbStatus,
                };
            }
            catch (Exception)
            {
                return new HealthStatus()
                {
                    Status = "Unhealthy",
                    Db = "Unhealthy",
                };
            }
        }
    }
}
