namespace GamersCommunity.Core.Events;

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
