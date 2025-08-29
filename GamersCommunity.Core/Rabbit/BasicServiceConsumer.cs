using GamersCommunity.Core.Exceptions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;

namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Base class for RabbitMQ service consumers that read messages from a queue
    /// and dispatch them (typically via a <c>TableRouter</c> in derived classes).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Holds the RabbitMQ connection factory and generic routing information
    /// (<see cref="EXCHANGE"/>, <see cref="QUEUE"/>). It does not open the connection
    /// by itself; derived classes usually call an initialization method and start
    /// the consume loop.
    /// </para>
    /// <para>
    /// Connection parameters are read from options bound to the <c>RabbitMQ</c> section
    /// (<c>Hostname</c>, <c>Username</c>, <c>Password</c>).
    /// </para>
    /// </remarks>
    /// <param name="opts">
    /// Options containing RabbitMQ settings (bound from the <c>RabbitMQ</c> configuration section).
    /// </param>
    /// <param name="tableRouter">
    /// Router used by consumers to dispatch messages to the appropriate table/action handlers.
    /// </param>
    /// <param name="logger">
    /// Logger of main service
    /// </param>
    /// <example>
    /// <code>
    /// public sealed class UsersConsumer : BasicServiceConsumer
    /// {
    ///     public UsersConsumer(IOptions<RabbitMQSettings> opts, TableRouter router)
    ///         : base(opts, router)
    ///     {
    ///         QUEUE = "users_queue";
    ///         EXCHANGE = "users_exchange"; // optional
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class BasicServiceConsumer(IOptions<RabbitMQSettings> opts, TableRouter tableRouter, ILogger logger)
    {
        /// <summary>
        /// Optional RabbitMQ exchange name the consumer queue may be bound to.
        /// Leave empty to use the default exchange.
        /// </summary>
        /// <remarks>
        /// Derived classes can override or set this value during construction to
        /// bind the queue to a specific exchange (e.g., a direct exchange for RPC).
        /// </remarks>
        public virtual string EXCHANGE { get; set; } = string.Empty;

        /// <summary>
        /// The name of the RabbitMQ queue to consume from.
        /// </summary>
        /// <remarks>
        /// Must be set (non-empty) by the derived class before starting the consumer.
        /// If the queue does not exist, the initialization logic declares it with
        /// durable/non-exclusive/non-auto-delete semantics.
        /// </remarks>
        public virtual string QUEUE { get; set; } = string.Empty;

        /// <summary>
        /// RabbitMQ connection factory built from options.
        /// </summary>
        private readonly ConnectionFactory _factory = new()
        {
            HostName = opts.Value.Hostname,
            UserName = opts.Value.Username,
            Password = opts.Value.Password,
        };

        /// <summary>
        /// Starts consuming messages from the configured queue and keeps the method alive
        /// until the provided <paramref name="ct"/> is cancelled. Per-message errors are
        /// logged and do not stop the consumer. Fatal connection errors are allowed to bubble up
        /// (they are handled/logged in <see cref="InitRabbitMQAsync(CancellationToken)"/>).
        /// </summary>
        /// <param name="ct">Cancellation token to stop the consumer gracefully.</param>
        /// <exception cref="InternalServerErrorException">
        /// Thrown when the queue name is null or empty.
        /// </exception>
        public async Task StartListeningAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(QUEUE))
                throw new InternalServerErrorException("Queue name must not be null or empty.");

            logger.Information("Starting consumer on host '{Host}' for queue '{Queue}'.", _factory.HostName, QUEUE);

            var channel = await InitRabbitMQAsync(ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    RabbitMQTableMessage? parsedMessage;
                    try
                    {
                        parsedMessage = JsonConvert.DeserializeObject<RabbitMQTableMessage>(message);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to deserialize incoming message. PayloadLength={Length}", body.Length);
                        return; // auto-ack is true; skip this message
                    }

                    if (parsedMessage == null)
                    {
                        logger.Error("Deserialized message is null. Skipping.");
                        return;
                    }

                    logger.Debug("Message received: table={Table}, action={Action}.", parsedMessage.Table, parsedMessage.Action);

                    string? response = null;
                    try
                    {
                        response = await tableRouter.RouteAsync(parsedMessage, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error while routing message (table={Table}, action={Action}).", parsedMessage.Table, parsedMessage.Action);
                    }

                    var props = ea.BasicProperties;
                    if (string.IsNullOrWhiteSpace(props?.ReplyTo))
                    {
                        logger.Warning("Missing ReplyTo for correlationId={CorrelationId}. Sender will not receive a response.", props?.CorrelationId);
                        return;
                    }

                    var replyProps = new BasicProperties
                    {
                        CorrelationId = props.CorrelationId
                    };

                    var responseBytes = Encoding.UTF8.GetBytes(response ?? string.Empty);
                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: props.ReplyTo,
                        mandatory: false,
                        basicProperties: replyProps,
                        body: responseBytes,
                        cancellationToken: ct
                    );

                    logger.Debug("Response sent to {ReplyTo} (correlationId={CorrelationId}).", props.ReplyTo, props.CorrelationId);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Graceful stop — no action needed
                }
                catch (Exception ex)
                {
                    // Never let the consumer crash due to a single bad message
                    logger.Error(ex, "Unhandled error while processing an incoming message.");
                }
            };

            // autoAck: true (keeps existing behavior)
            var consumerTag = await channel.BasicConsumeAsync(queue: QUEUE!, autoAck: true, consumer: consumer, cancellationToken: ct);

            try
            {
                // Keep the consumer alive until cancellation is requested
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                // Attempt to cancel cleanly
                try
                {
                    await channel.BasicCancelAsync(consumerTag, cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Error while cancelling RabbitMQ consumer {Tag}.", consumerTag);
                }

                logger.Information("Consumer '{Tag}' cancelled.", consumerTag);
            }
        }

        /// <summary>
        /// Creates the RabbitMQ connection and channel, declares the queue,
        /// and returns an open channel ready to consume or publish RPC responses.
        /// Logs and rethrows any fatal connection errors to allow the host/container to fail fast.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An open <see cref="IChannel"/> bound to the configured queue.</returns>
        /// <exception cref="InternalServerErrorException">
        /// Thrown when the queue name is null or empty.
        /// </exception>
        private async Task<IChannel> InitRabbitMQAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(QUEUE))
                throw new InternalServerErrorException("Queue name must not be null or empty.");

            try
            {
                logger.Debug("Opening RabbitMQ connection to {Host}...", _factory.HostName);
                var connection = await _factory.CreateConnectionAsync(ct);
                var channel = await connection.CreateChannelAsync(cancellationToken: ct);

                await channel.QueueDeclareAsync(
                    queue: QUEUE!,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: ct
                );

                logger.Information("RabbitMQ channel ready. Queue '{Queue}' declared (durable=true).", QUEUE);
                return channel;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                logger.Information("RabbitMQ initialization cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Failed to initialize RabbitMQ connection/channel (host={Host}, queue={Queue}).", _factory.HostName, QUEUE);
                throw;
            }
        }
    }
}