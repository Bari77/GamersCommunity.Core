namespace GamersCommunity.Core.Events;

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
