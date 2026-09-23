using GamersCommunity.Core.Rabbit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GamersCommunity.Core.Hosting;

/// <summary>
/// Background worker that runs a <see cref="BasicServiceConsumer"/> with broker reconnect.
/// </summary>
public sealed class ConsumerWorker<TConsumer>(IServiceScopeFactory scopeFactory, ILogger logger)
    : BackgroundService
    where TConsumer : BasicServiceConsumer
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await consumer.StartListeningAsync(ct);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                logger.Information("ConsumerWorker stopping (cancellation requested).");
                return;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "RabbitMQ communication error. Retrying in {Delay}s.", ReconnectDelay.TotalSeconds);
                try
                {
                    await Task.Delay(ReconnectDelay, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    logger.Information("ConsumerWorker stopping (cancellation requested).");
                    return;
                }
            }
        }
    }
}
