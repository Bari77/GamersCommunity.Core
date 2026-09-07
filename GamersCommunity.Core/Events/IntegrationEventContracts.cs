namespace GamersCommunity.Core.Events
{
    /// <summary>
    /// Fanout exchanges used to broadcast integration events between microservices.
    /// </summary>
    /// <remarks>
    /// Publishers do not know their subscribers: they publish once on the exchange, and each
    /// interested microservice binds its own durable queue to it. Adding a new subscriber therefore
    /// requires no change on the publishing side.
    /// </remarks>
    public static class IntegrationExchanges
    {
        /// <summary>
        /// Broadcasts state owned by the Platform microservice (identity, presence, ...).
        /// </summary>
        public const string PlatformEvents = "platform_events";
    }

    /// <summary>
    /// Naming convention for the queues subscribers bind to a fanout exchange.
    /// </summary>
    /// <remarks>
    /// A queue delivers each message to a single consumer, so subscribers cannot share one: two
    /// microservices bound to the same queue would each receive only half of the events. Every
    /// microservice therefore owns a queue derived from the exchange name and its own identifier.
    /// The publisher never references these names.
    /// </remarks>
    public static class IntegrationQueues
    {
        /// <summary>
        /// Builds the queue name a microservice must bind to an exchange, e.g.
        /// <c>platform_events_worldofwarcraft</c>.
        /// </summary>
        /// <param name="exchange">Fanout exchange to subscribe to. See <see cref="IntegrationExchanges"/>.</param>
        /// <param name="microserviceId">Identifier of the subscribing microservice, as declared in the gateway routing.</param>
        public static string ForMicroservice(string exchange, string microserviceId)
            => $"{exchange}_{microserviceId}";
    }

    /// <summary>
    /// Discriminator values carried by the <c>type</c> property of every integration event.
    /// </summary>
    public static class IntegrationEventTypes
    {
        public const string UserIdentityChanged = "user.identity.changed";
    }

    /// <summary>
    /// Base shape shared by every integration event travelling on a fanout exchange.
    /// </summary>
    public abstract record IntegrationEvent
    {
        /// <summary>
        /// Discriminator used by subscribers to dispatch the payload. See <see cref="IntegrationEventTypes"/>.
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        /// Unique identifier of this emission, usable for deduplication.
        /// </summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// UTC instant at which the change was published. Subscribers must ignore an event older than
        /// the state they already hold, since delivery is at-least-once and unordered on redelivery.
        /// </summary>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Published by the Platform microservice whenever the public identity of a user changes.
    /// </summary>
    /// <remarks>
    /// Carries display data only. Roles, sanctions and any other authorization state are deliberately
    /// excluded: they must stay at their source of truth and never be read from a replicated copy.
    /// </remarks>
    public sealed record UserIdentityChangedEvent : IntegrationEvent
    {
        public override string Type => IntegrationEventTypes.UserIdentityChanged;

        public Guid UserPublicId { get; init; }

        public string Nickname { get; init; } = string.Empty;

        public string Discriminator { get; init; } = string.Empty;

        public string AvatarUrl { get; init; } = string.Empty;
    }
}
