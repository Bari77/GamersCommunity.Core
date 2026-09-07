using GamersCommunity.Core.Rabbit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;

namespace GamersCommunity.Core.Events
{
    /// <summary>
    /// Background service that binds a microservice-owned durable queue to a fanout exchange
    /// and dispatches every received integration event to <see cref="HandleAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each subscriber owns its queue, so a microservice that is down accumulates events and catches up
    /// on restart instead of losing them.
    /// </para>
    /// <para>
    /// Delivery is at-least-once: implementations of <see cref="HandleAsync"/> must be idempotent and
    /// must discard an event older than the state they already hold.
    /// </para>
    /// </remarks>
    public abstract class IntegrationEventSubscriber(IOptions<RabbitMQSettings> opts, ILogger logger) : BackgroundService
    {
        private const ushort PrefetchCount = 10;
        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(3);

        private readonly ConnectionFactory _factory = new()
        {
            HostName = opts.Value.Hostname,
            UserName = opts.Value.Username,
            Password = opts.Value.Password,
        };

        /// <summary>
        /// Fanout exchange to subscribe to. See <see cref="IntegrationExchanges"/>.
        /// </summary>
        protected abstract string Exchange { get; }

        /// <summary>
        /// Durable queue owned by this microservice. Build it with
        /// <see cref="IntegrationQueues.ForMicroservice"/> so every game follows the same convention.
        /// </summary>
        protected abstract string Queue { get; }

        /// <summary>
        /// Handles one event. Unknown types should be ignored rather than throwing, so that a publisher
        /// can introduce new events without breaking existing subscribers.
        /// </summary>
        /// <param name="type">Value of the <c>type</c> discriminator.</param>
        /// <param name="json">Raw JSON payload.</param>
        protected abstract Task HandleAsync(string type, string json, CancellationToken ct);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var connection = await _factory.CreateConnectionAsync(stoppingToken);
                    await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                    await channel.ExchangeDeclareAsync(
                        exchange: Exchange,
                        type: ExchangeType.Fanout,
                        durable: true,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: stoppingToken);

                    await channel.QueueDeclareAsync(
                        queue: Queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: stoppingToken);

                    await channel.QueueBindAsync(
                        queue: Queue,
                        exchange: Exchange,
                        routingKey: string.Empty,
                        arguments: null,
                        cancellationToken: stoppingToken);

                    await channel.BasicQosAsync(0, PrefetchCount, false, stoppingToken);

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += async (_, ea) =>
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        try
                        {
                            var type = IntegrationEventSerializer.ReadType(json);
                            if (string.IsNullOrWhiteSpace(type))
                            {
                                logger.Warning("Integration event without a 'type' discriminator on '{Queue}'.", Queue);
                            }
                            else
                            {
                                await HandleAsync(type, json, stoppingToken);
                            }

                            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Failed to handle integration event on '{Queue}'.", Queue);
                            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                        }
                    };

                    await channel.BasicConsumeAsync(
                        queue: Queue,
                        autoAck: false,
                        consumer: consumer,
                        cancellationToken: stoppingToken);

                    logger.Information("Subscribed to '{Exchange}' through queue '{Queue}'.", Exchange, Queue);

                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Subscriber on '{Queue}' disconnected; retrying in {Delay}s.", Queue, ReconnectDelay.TotalSeconds);
                    try
                    {
                        await Task.Delay(ReconnectDelay, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
    }
}
