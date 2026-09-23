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

        public async Task<string> CallAsync(string queue, string message, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(queue))
                throw new BadRequestException("QUEUE_NULL", "Queue name must not be null or empty.");
            if (string.IsNullOrWhiteSpace(message))
                throw new BadRequestException("MESSAGE_NULL", "Message must not be null or empty.");

            var conn = await EnsureConnectionAsync(ct);
            await using var ch = await conn.CreateChannelAsync(cancellationToken: ct);

            var replyQueue = await ch.QueueDeclareAsync(
                queue: string.Empty,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null,
                cancellationToken: ct);

            var correlationId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            string consumerTag = string.Empty;

            var consumer = new AsyncEventingBasicConsumer(ch);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    if (ea.BasicProperties?.CorrelationId != correlationId)
                        return;

                    var responseJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                    logger.Debug("RPC response received (corrId={CorrelationId}).", correlationId);

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
                catch (Exception ex)
                {
                    logger.Error(ex, "Error while handling RPC response (corrId={CorrelationId}).", correlationId);
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
                        logger.Warning(ackEx, "Failed to ACK RPC response (corrId={CorrelationId}).", correlationId);
                    }
                }
            };

            try
            {
                consumerTag = await ch.BasicConsumeAsync(
                    queue: replyQueue.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: ct);

                var props = new BasicProperties
                {
                    CorrelationId = correlationId,
                    ReplyTo = replyQueue.QueueName,
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };

                logger.Debug("Publishing RPC message to '{Queue}' (corrId={CorrelationId}).", queue, correlationId);

                await ch.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queue,
                    mandatory: false,
                    basicProperties: props,
                    body: Encoding.UTF8.GetBytes(message),
                    cancellationToken: ct);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(opts.Value.Timeout));

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token));
                if (completed == tcs.Task)
                    return await tcs.Task.ConfigureAwait(false);

                throw new GatewayTimeoutException("TIMEOUT", $"No response received within the timeout period ({opts.Value.Timeout}s).");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(consumerTag))
                {
                    try
                    {
                        await ch.BasicCancelAsync(consumerTag, cancellationToken: CancellationToken.None);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Ensures there is an open RabbitMQ connection and channel, creating them if necessary.
        /// Logs and rethrows fatal errors to allow the host/container to fail fast.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An open AMQP channel.</returns>
        private async Task<IConnection> EnsureConnectionAsync(CancellationToken ct = default)
        {
            if (Connection is null || !Connection.IsOpen)
            {
                logger.Information("Opening RabbitMQ connection to {Host}...", Factory.HostName);
                Connection = await Factory.CreateConnectionAsync(ct);
                logger.Information("RabbitMQ connection established.");
            }

            return Connection;
        }
    }
}
