using GamersCommunity.Core.Enums;

namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Represents a generic message dispatched through the message bus.
    /// </summary>
    public sealed class BusMessage
    {
        /// <summary>
        /// The category of the target service (Table, Feature, etc.).
        /// </summary>
        public BusServiceTypeEnum Type { get; init; }

        /// <summary>
        /// The name of the target resource (e.g. "Users", "AppSettings").
        /// </summary>
        public string Resource { get; init; } = default!;

        /// <summary>
        /// The action to execute (GET, LIST, UPDATE, etc.).
        /// </summary>
        public string Action { get; init; } = default!;

        /// <summary>
        /// Optional serialized data payload.
        /// </summary>
        public string? Data { get; init; }

        /// <summary>
        /// Optional identifier for the target resource.
        /// </summary>
        public int? Id { get; init; }
    }
}
