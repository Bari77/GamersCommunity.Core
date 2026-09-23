using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Serilog;

namespace GamersCommunity.Core.Realtime;

public interface IRealtimeEventPublisher
{
    Task PublishAsync<T>(T payload, CancellationToken ct = default);
}

/// <summary>
/// Publishes JSON realtime events to <see cref="RealtimeQueues.Gateway"/>.
/// </summary>
public sealed class RealtimeEventPublisher(IOptions<RabbitMQSettings> opts, ILogger logger)
    : IRealtimeEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new UtcDateTimeJsonConverter() },
    };

    private readonly ConnectionFactory _factory = new()
    {
        HostName = opts.Value.Hostname,
        UserName = opts.Value.Username,
        Password = opts.Value.Password,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<T>(T payload, CancellationToken ct = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts));
        var channel = await EnsureChannelAsync(ct);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: RealtimeQueues.Gateway,
            mandatory: false,
            basicProperties: new BasicProperties
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent,
            },
            body: body,
            cancellationToken: ct);

        logger.Debug("Published realtime event to '{Queue}'.", RealtimeQueues.Gateway);
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

    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _gate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is null || !_connection.IsOpen)
                _connection = await _factory.CreateConnectionAsync(ct);

            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.QueueDeclareAsync(
                queue: RealtimeQueues.Gateway,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }
}
