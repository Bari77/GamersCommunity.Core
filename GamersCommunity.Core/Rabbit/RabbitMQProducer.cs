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
    /// RPC-style RabbitMQ producer that publishes a message to a target queue and awaits a reply
    /// on a temporary, exclusive reply queue identified by a correlation id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each request, the producer declares a server-named <c>exclusive</c> + <c>auto-delete</c> reply queue,
    /// consumes from it, and returns the payload from the JSON <see cref="RpcEnvelope{T}"/> when <c>ok=true</c>.
    /// If <c>ok=false</c>, it throws an <see cref="RpcException"/> built from the returned <see cref="RpcError"/>.
    /// </para>
    /// </remarks>
    /// <param name="opts">RabbitMQ settings.</param>
    /// <param name="logger">Application logger (Serilog).</param>
    public class RabbitMQProducer(IOptions<RabbitMQSettings> opts, ILogger logger)
    {
        private readonly ConnectionFactory Factory = new()
        {
            HostName = opts.Value.Hostname,
            UserName = opts.Value.Username,
            Password = opts.Value.Password
        };

        private IConnection? Connection;
        private IChannel? Channel;

        /// <summary>
        /// Publishes a message to the given queue using a fresh correlation id and a server-named reply queue.
        /// The returned <see cref="BasicProperties"/> contains the <c>CorrelationId</c> and the <c>ReplyTo</c> queue name.
        /// </summary>
        /// <param name="queue">Target routing queue.</param>
        /// <param name="message">Opaque payload (typically JSON).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>AMQP properties including <c>CorrelationId</c> and <c>ReplyTo</c>.</returns>
        /// <exception cref="BadRequestException">Thrown when the queue or the message is invalid.</exception>
        public async Task<BasicProperties> SendMessageAsync(string queue, string message, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(queue))
                throw new BadRequestException("QUEUE_NULL", "Queue name must not be null or empty.");
            if (string.IsNullOrWhiteSpace(message))
                throw new BadRequestException("MESSAGE_NULL", "Message must not be null or empty.");

            var ch = await InitRabbitMQ();

            var body = Encoding.UTF8.GetBytes(message);

            var replyQueue = await ch.QueueDeclareAsync(
                queue: string.Empty,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null,
                cancellationToken: ct);

            var props = new BasicProperties
            {
                CorrelationId = Guid.NewGuid().ToString("N"),
                ReplyTo = replyQueue.QueueName,
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            logger.Debug("Publishing RPC message to '{Queue}' (corrId={CorrelationId}).", queue, props.CorrelationId);

            await ch.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            return props;
        }

        /// <summary>
        /// Waits for the RPC response matching the provided <paramref name="props"/> correlation id
        /// on the corresponding <paramref name="props"/> reply queue.
        /// </summary>
        /// <param name="props">AMQP properties returned by <see cref="SendMessageAsync(string, string, CancellationToken)"/>.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The string payload from the <see cref="RpcEnvelope{T}"/> when <c>ok=true</c>.</returns>
        /// <exception cref="GatewayTimeoutException">Thrown when no response is received within the timeout.</exception>
        /// <exception cref="InternalServerErrorException">Thrown when <paramref name="props"/> are incomplete.</exception>
        /// <exception cref="RpcException">Thrown when the consumer responded with <c>ok=false</c>.</exception>
        public async Task<string> GetResponseAsync(BasicProperties props, CancellationToken ct = default)
        {
            if (props is null)
                throw new InternalServerErrorException("AMQP_NULL", "AMQP properties must not be null.");
            if (string.IsNullOrWhiteSpace(props.CorrelationId))
                throw new InternalServerErrorException("CORRELATION_NULL", "CorrelationId must not be null or empty.");
            if (string.IsNullOrWhiteSpace(props.ReplyTo))
                throw new InternalServerErrorException("REPLY_TO_NULL", "ReplyTo must not be null or empty.");

            var ch = await InitRabbitMQ();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            string consumerTag = string.Empty;

            var consumer = new AsyncEventingBasicConsumer(ch);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    if (ea.BasicProperties?.CorrelationId == props.CorrelationId)
                    {
                        var responseJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                        logger.Debug("RPC response received (corrId={CorrelationId}).", props.CorrelationId);

                        try
                        {
                            var envelope = JsonConvert.DeserializeObject<RpcEnvelope<string?>>(responseJson);
                            if (envelope is null)
                                throw new RpcException("INVALID_RESPONSE", "Response cannot be deserialized.", responseJson);

                            if (!envelope.Ok)
                                throw new RpcException(
                                    envelope.Error?.Code ?? "ERROR",
                                    envelope.Error?.Message ?? "Unknown error",
                                    envelope.Error?.Details);

                            tcs.TrySetResult(envelope.Data ?? string.Empty);
                        }
                        catch (JsonException jex)
                        {
                            logger.Warning(jex, "Response is not a valid envelope. Returning raw body.");
                            tcs.TrySetResult(responseJson);
                        }
                        catch (RpcException rex)
                        {
                            tcs.TrySetException(rex);
                        }
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
                        await ch.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                    }
                    catch (Exception ackEx)
                    {
                        logger.Warning(ackEx, "Failed to ACK RPC response (corrId={CorrelationId}).", props.CorrelationId);
                    }
                }
            };

            try
            {
                logger.Debug("Waiting RPC response on '{ReplyTo}' (corrId={CorrelationId}, timeout={Timeout}s)...", props.ReplyTo, props.CorrelationId, opts.Value.Timeout);

                consumerTag = await ch.BasicConsumeAsync(
                    queue: props.ReplyTo!,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: ct);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(opts.Value.Timeout));

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token));
                if (completed == tcs.Task)
                {
                    try
                    {
                        await ch.BasicCancelAsync(consumerTag, cancellationToken: ct);
                    }
                    catch (Exception cancelEx)
                    {
                        logger.Warning(cancelEx, "Failed to cancel RPC consumer: tag={Tag}.", consumerTag);
                    }

                    return await tcs.Task.ConfigureAwait(false);
                }

                throw new GatewayTimeoutException("TIMEOUT", $"No response received within the timeout period ({opts.Value.Timeout}s).");
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(consumerTag))
                        await Channel!.BasicCancelAsync(consumerTag, cancellationToken: ct);
                }
                catch
                {
                }

                try
                {
                    await ch.QueueDeleteAsync(props.ReplyTo!, ifUnused: false, ifEmpty: false, cancellationToken: ct);
                }
                catch (Exception delEx)
                {
                    logger.Debug(delEx, "Failed to delete temporary reply queue '{ReplyTo}'.", props.ReplyTo);
                }
            }
        }

        /// <summary>
        /// Ensures there is an open RabbitMQ connection and channel, creating them if necessary.
        /// Logs and rethrows fatal errors to allow the host/container to fail fast.
        /// </summary>
        /// <returns>An open AMQP channel.</returns>
        private async Task<IChannel> InitRabbitMQ()
        {
            try
            {
                if (Connection is null || !Connection.IsOpen)
                {
                    logger.Information("Opening RabbitMQ connection to {Host}...", Factory.HostName);
                    Connection = await Factory.CreateConnectionAsync();
                    logger.Information("RabbitMQ connection established.");
                }

                if (Channel is null || !Channel.IsOpen)
                {
                    Channel = await Connection.CreateChannelAsync();
                    logger.Information("RabbitMQ channel created.");
                }

                return Channel;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Failed to initialize RabbitMQ (host={Host}).", Factory.HostName);
                throw;
            }
        }
    }
}
