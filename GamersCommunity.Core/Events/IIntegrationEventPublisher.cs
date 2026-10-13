namespace GamersCommunity.Core.Events;

/// <summary>
/// Broadcasts integration events on a fanout exchange.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(string exchange, IntegrationEvent integrationEvent, CancellationToken ct = default);
}
