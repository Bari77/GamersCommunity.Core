using GamersCommunity.Core.Rabbit;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Serilog;
using System.Text;

namespace GamersCommunity.Core.Events
{
    /// <summary>
    /// Broadcasts integration events on a fanout exchange.
    /// </summary>
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(string exchange, IntegrationEvent integrationEvent, CancellationToken ct = default);
    }

    /// <summary>
    /// RabbitMQ implementation of <see cref="IIntegrationEventPublisher"/>.
    /// </summary>
    /// <remarks>
    /// Register as a singleton: the connection and channel are reused across publications.
    /// </remarks>
    public sealed class RabbitIntegrationEventPublisher(IOptions<RabbitMQSettings> opts, ILogger logger)
        : IIntegrationEventPublisher, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory = new()
        {
            HostName = opts.Value.Hostname,
            UserName = opts.Value.Username,
            Password = opts.Value.Password,
        };

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly HashSet<string> _declaredExchanges = [];
        private IConnection? _connection;
        private IChannel? _channel;

        public async Task PublishAsync(string exchange, IntegrationEvent integrationEvent, CancellationToken ct = default)
        {
            var channel = await EnsureChannelAsync(exchange, ct);
            var body = Encoding.UTF8.GetBytes(IntegrationEventSerializer.Serialize((object)integrationEvent));

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8",
                    DeliveryMode = DeliveryModes.Persistent,
                    MessageId = integrationEvent.EventId.ToString("N"),
                    Type = integrationEvent.Type,
                },
                body: body,
                cancellationToken: ct);

            logger.Debug(
                "Published integration event '{Type}' on exchange '{Exchange}' (eventId={EventId}).",
                integrationEvent.Type,
                exchange,
                integrationEvent.EventId);
        }

        public async ValueTask DisposeAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (_channel is not null)
                {
                    await _channel.DisposeAsync();
                    _channel = null;
                }

                if (_connection is not null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }
            }
            finally
            {
                _gate.Release();
                _gate.Dispose();
            }
        }

        private async Task<IChannel> EnsureChannelAsync(string exchange, CancellationToken ct)
        {
            if (_channel is { IsOpen: true } && _declaredExchanges.Contains(exchange))
                return _channel;

            await _gate.WaitAsync(ct);
            try
            {
                if (_channel is not { IsOpen: true })
                {
                    _declaredExchanges.Clear();

                    if (_connection is null || !_connection.IsOpen)
                        _connection = await _factory.CreateConnectionAsync(ct);

                    _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
                }

                if (_declaredExchanges.Add(exchange))
                {
                    await _channel.ExchangeDeclareAsync(
                        exchange: exchange,
                        type: ExchangeType.Fanout,
                        durable: true,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: ct);
                }

                return _channel;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
