namespace GamersCommunity.Core.Events;

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
