using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Rabbit;

namespace GamersCommunity.Core.Services
{
    /// <summary>
    /// Base contract for services that can handle bus messages.
    /// </summary>
    public interface IBusService
    {
        /// <summary>
        /// The type category of this service (Table, Feature, etc.).
        /// </summary>
        BusServiceTypeEnum Type { get; }

        /// <summary>
        /// The logical name of the resource handled by this service.
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// Dispatches the requested <paramref name="message"/> to the corresponding CRUD method and
        /// returns a JSON-serialized result.
        /// </summary>
        /// <param name="message">Message to dispatch.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>JSON string containing the operation result.</returns>
        /// <exception cref="BadRequestException">
        /// Thrown when required inputs are missing or cannot be parsed from <paramref name="message.Data"/>.
        /// </exception>
        /// <exception cref="InternalServerErrorException">
        /// Thrown when <paramref name="message.Action"/> is not recognized/implemented.
        /// </exception>
        Task<string> HandleAsync(BusMessage message, CancellationToken ct = default);
    }

}
