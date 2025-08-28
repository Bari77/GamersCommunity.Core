using GamersCommunity.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;

namespace GamersCommunity.Core.Rabbit
{
    public class RabbitMQProducer
    {
        private readonly long SECONDS_BEFORE_CANCEL = 30;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQProducer(IConfiguration configuration)
        {
            var rabbitMQConfig = configuration.GetSection("RabbitMQ");
            _factory = new ConnectionFactory
            {
                HostName = rabbitMQConfig["HostName"]!,
                UserName = rabbitMQConfig["UserName"]!,
                Password = rabbitMQConfig["Password"]!
            };
        }

        public async Task<BasicProperties> SendMessageAsync(string queue, string message)
        {
            var channel = await InitRabbitMQ();

            var body = Encoding.UTF8.GetBytes(message);

            var replyQueue = await channel.QueueDeclareAsync();
            var props = new BasicProperties
            {
                CorrelationId = Guid.NewGuid().ToString(),
                ReplyTo = replyQueue.QueueName
            };

            Log.Debug($"Send message on {queue}...");
            await channel.BasicPublishAsync(string.Empty, queue, false, props, body);

            return props;
        }

        public async Task<string> GetResponseAsync(BasicProperties props)
        {
            var channel = await InitRabbitMQ();
            var tcs = new TaskCompletionSource<string>();

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == props.CorrelationId)
                {
                    var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                    tcs.SetResult(response);

                    Log.Debug($"Got a response for {props.CorrelationId}");
                }

                // Accusé de réception
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            try
            {
                Log.Debug($"Wait a response for {props.CorrelationId} on {props.ReplyTo} key...");
                var consumerTag = await channel.BasicConsumeAsync(props.ReplyTo!, false, consumer);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(SECONDS_BEFORE_CANCEL));
                var task = tcs.Task;
                var completedTask = await Task.WhenAny(task, Task.Delay(-1, cts.Token));

                if (completedTask == task)
                {
                    await channel.BasicCancelAsync(consumerTag);
                    return await task;
                }

                throw new GatewayTimeoutException($"No response received within the timeout period ({SECONDS_BEFORE_CANCEL}s).");
            }
            finally
            {
                await channel.QueueDeleteAsync(props.ReplyTo!);
            }
        }

        private async Task<IChannel> InitRabbitMQ()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _factory.CreateConnectionAsync();
            }

            if (_channel == null || !_channel.IsOpen)
            {
                _channel = await _connection.CreateChannelAsync();

                Log.Information($"RabbitMQ initialized");
            }

            return _channel;
        }
    }
}
