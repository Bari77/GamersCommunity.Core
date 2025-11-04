using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Services;

namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Central router that dispatches incoming <see cref="BusMessage"/> instances
    /// to the appropriate <see cref="IBusService"/> implementation.
    /// </summary>
    public sealed class BusRouter(IEnumerable<IBusService> services)
    {
        /// <summary>
        /// Routes a message to the matching service based on <see cref="BusMessage.Type"/> and <see cref="BusMessage.Resource"/>.
        /// </summary>
        /// <exception cref="NotFoundException">Thrown when no suitable service is registered.</exception>
        public async Task<string> RouteAsync(BusMessage message, CancellationToken ct = default)
        {
            var service = services.FirstOrDefault(s =>
                s.Type == message.Type &&
                s.Resource.Equals(message.Resource, StringComparison.OrdinalIgnoreCase));

            if (service is null)
                throw new NotFoundException("SERVICE_NOT_FOUND", $"No service found for {message.Type}/{message.Resource}");

            return await service.HandleAsync(message, ct);
        }
    }
}