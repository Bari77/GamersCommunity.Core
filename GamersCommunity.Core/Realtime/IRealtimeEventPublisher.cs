namespace GamersCommunity.Core.Realtime;

public interface IRealtimeEventPublisher
{
    Task PublishAsync<T>(T payload, CancellationToken ct = default);
}
