using GamersCommunity.Core.Exceptions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;

namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// RPC-style RabbitMQ producer that publishes a message to a target queue and awaits a reply
    /// on a temporary, exclusive reply queue identified by a correlation id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The producer maintains a shared connection/channel and, for each request,
    /// declares a server-named <c>exclusive</c> + <c>auto-delete</c> reply queue.
    /// It then consumes from that queue (manual ack) until the expected correlation id arrives
    /// or the timeout elapses.
    /// </para>
    /// <para>
    /// Reliability notes:
    /// - Validates inputs (queue, message) and throws early on misconfiguration.
    /// - Logs and rethrows fatal connection errors (so an orchestrator can restart the service).
    /// - Ensures consumer is cancelled and the temporary reply queue is deleted in all paths.
    /// </para>
    /// </remarks>
    /// <param name="opts">Options bound from the <c>RabbitMQ</c> section.</param>
    /// <param name="logger">Application logger (untyped).</param>
    public class RabbitMQProducer(IOptions<RabbitMQSettings> opts, ILogger logger)
    {
        /// <summary>
        /// Default RPC timeout (in seconds) used by <see cref="GetResponseAsync(BasicProperties, CancellationToken)"/>.
        /// </summary>
        private readonly long SECONDS_BEFORE_CANCEL = 30;

        /// <summary>
        /// RabbitMQ connection factory built from options.
        /// </summary>
        private readonly ConnectionFactory _factory = new()
        {
            HostName = opts.Value.Hostname,
            UserName = opts.Value.Username,
            Password = opts.Value.Password,
        };

        private IConnection? _connection;
        private IChannel? _channel;

        /// <summary>
        /// Publishes a message to <paramref name="queue"/> and returns the AMQP properties
        /// containing the generated correlation id and the dedicated <c>ReplyTo</c> queue to listen on.
        /// </summary>
        /// <param name="queue">Target routing queue.</param>
        /// <param name="message">Opaque payload (typically JSON).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>AMQP properties with <see cref="BasicProperties.CorrelationId"/> and <see cref="BasicProperties.ReplyTo"/>.</returns>
        /// <exception cref="BadRequestException">Thrown when <paramref name="queue"/> or <paramref name="message"/> are invalid.</exception>
        public async Task<BasicProperties> SendMessageAsync(string queue, string message, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(queue))
                throw new BadRequestException("Queue name must not be null or empty.");
            if (string.IsNullOrWhiteSpace(message))
                throw new BadRequestException("Message must not be null or empty.");

            var channel = await InitRabbitMQ();

            var body = Encoding.UTF8.GetBytes(message);

            // Temporary, exclusive reply queue (server-named)
            var replyQueue = await channel.QueueDeclareAsync(
                queue: string.Empty,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null,
                cancellationToken: ct);

            var props = new BasicProperties
            {
                CorrelationId = Guid.NewGuid().ToString("N"),
                ReplyTo = replyQueue.QueueName
            };

            logger.Debug("Publishing RPC message to '{Queue}' (corrId={CorrelationId}).", queue, props.CorrelationId);
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            return props;
        }

        /// <summary>
        /// Waits for the RPC response matching <paramref name="props"/>.<see cref="BasicProperties.CorrelationId"/>
        /// on <paramref name="props"/>.<see cref="BasicProperties.ReplyTo"/>, with a default timeout.
        /// </summary>
        /// <param name="props">AMQP properties returned by <see cref="SendMessageAsync(string, string, CancellationToken)"/>.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The response payload as a string.</returns>
        /// <exception cref="GatewayTimeoutException">Thrown when no response is received within the timeout.</exception>
        /// <exception cref="InternalServerErrorException">Thrown when <paramref name="props"/> are incomplete.</exception>
        public async Task<string> GetResponseAsync(BasicProperties props, CancellationToken ct = default)
        {
            if (props is null)
                throw new InternalServerErrorException("AMQP properties must not be null.");
            if (string.IsNullOrWhiteSpace(props.CorrelationId))
                throw new InternalServerErrorException("CorrelationId must not be null or empty.");
            if (string.IsNullOrWhiteSpace(props.ReplyTo))
                throw new InternalServerErrorException("ReplyTo must not be null or empty.");

            var channel = await InitRabbitMQ();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            string consumerTag = string.Empty;

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    if (ea.BasicProperties?.CorrelationId == props.CorrelationId)
                    {
                        var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                        logger.Debug("RPC response received (corrId={CorrelationId}).", props.CorrelationId);
                        tcs.TrySetResult(response);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error while handling RPC response (corrId={CorrelationId}).", props.CorrelationId);
                    tcs.TrySetException(ex);
                }
                finally
                {
                    try
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                    }
                    catch (Exception ackEx)
                    {
                        logger.Warning(ackEx, "Failed to ACK RPC response (corrId={CorrelationId}).", props.CorrelationId);
                    }
                }
            };

            try
            {
                logger.Debug("Waiting RPC response on '{ReplyTo}' (corrId={CorrelationId}, timeout={Timeout}s)...",
                    props.ReplyTo, props.CorrelationId, SECONDS_BEFORE_CANCEL);

                consumerTag = await channel.BasicConsumeAsync(
                    queue: props.ReplyTo!,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: ct);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(SECONDS_BEFORE_CANCEL));

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token));
                if (completed == tcs.Task)
                {
                    try
                    {
                        await channel.BasicCancelAsync(consumerTag, cancellationToken: ct);
                    }
                    catch (Exception cancelEx)
                    {
                        logger.Warning(cancelEx, "Failed to cancel RPC consumer (tag={Tag}).", consumerTag);
                    }

                    return await tcs.Task.ConfigureAwait(false);
                }

                throw new GatewayTimeoutException($"No response received within the timeout period ({SECONDS_BEFORE_CANCEL}s).");
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(consumerTag))
                        await channel.BasicCancelAsync(consumerTag, cancellationToken: ct);
                }
                catch { /* best effort */ }

                // Best-effort cleanup of the temporary reply queue
                try
                {
                    await channel.QueueDeleteAsync(props.ReplyTo!, ifUnused: false, ifEmpty: false, cancellationToken: ct);
                }
                catch (Exception delEx)
                {
                    logger.Debug(delEx, "Failed to delete temporary reply queue '{ReplyTo}'.", props.ReplyTo);
                }
            }
        }

        /// <summary>
        /// Ensures there is an open connection and channel, creating them if necessary.
        /// Logs and rethrows fatal errors to allow the host/container to fail fast.
        /// </summary>
        /// <returns>An open AMQP channel.</returns>
        private async Task<IChannel> InitRabbitMQ()
        {
            try
            {
                if (_connection is null || !_connection.IsOpen)
                {
                    logger.Information("Opening RabbitMQ connection to {Host}...", _factory.HostName);
                    _connection = await _factory.CreateConnectionAsync();
                    logger.Information("RabbitMQ connection established.");
                }

                if (_channel is null || !_channel.IsOpen)
                {
                    _channel = await _connection.CreateChannelAsync();
                    logger.Information("RabbitMQ channel created.");
                }

                return _channel;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Failed to initialize RabbitMQ (host={Host}).", _factory.HostName);
                throw;
            }
        }
    }
}
