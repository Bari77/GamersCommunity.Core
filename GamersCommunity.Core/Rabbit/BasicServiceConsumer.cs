using GamersCommunity.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;

namespace GamersCommunity.Core.Rabbit
{
    public abstract class BasicServiceConsumer
    {
        public virtual string EXCHANGE { get; set; } = string.Empty;
        public virtual string QUEUE { get; set; } = string.Empty;

        private readonly ConnectionFactory _factory;
        private readonly TableRouter _tableRouter;

        public BasicServiceConsumer(IConfiguration configuration, TableRouter tableRouter)
        {
            var rabbitMQConfig = configuration.GetSection("RabbitMQ");
            _factory = new ConnectionFactory
            {
                HostName = rabbitMQConfig["HostName"]!,
                UserName = rabbitMQConfig["UserName"]!,
                Password = rabbitMQConfig["Password"]!
            };

            _tableRouter = tableRouter;
        }

        public async Task StartListeningAsync()
        {
            Log.Information($"StartListeningAsync on {_factory.HostName} host name");
            var channel = await InitRabbitMQ();

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                RabbitMQTableMessage? parsedMessage;

                parsedMessage = JsonConvert.DeserializeObject<RabbitMQTableMessage>(message);

                if (parsedMessage == null)
                {
                    throw new InternalServerErrorException("Parsed message null");
                }

                Log.Debug($"Message received for table {parsedMessage.Table}, action {parsedMessage.Action}");

                var response = await _tableRouter.RouteAsync(parsedMessage);

                var props = ea.BasicProperties;
                if (string.IsNullOrEmpty(props.ReplyTo))
                {
                    throw new InternalServerErrorException("ReplyTo is null or empty");
                }

                var replyProps = new BasicProperties
                {
                    CorrelationId = props.CorrelationId
                };

                var responseBytes = Encoding.UTF8.GetBytes(response);

                await channel.BasicPublishAsync(string.Empty, props.ReplyTo, false, replyProps, responseBytes);

                Log.Debug($"Response sent to {props.ReplyTo} for {props.CorrelationId}");
            };

            await channel.BasicConsumeAsync(QUEUE!, true, consumer);
        }

        private async Task<IChannel> InitRabbitMQ()
        {
            var connection = await _factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(QUEUE!, true, false, false);

            Log.Information($"InitRabbitMQ - Queue \"{QUEUE}\"");

            return channel;
        }
    }
}
