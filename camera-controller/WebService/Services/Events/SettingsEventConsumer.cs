using System.Text;
using System.Text.Json;
using Lightview.Shared.Contracts.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using RabbitMQShared.Configuration;
using RabbitMQShared.Services;

namespace WebService.Services.Events;

/// <summary>
/// Consumes settings-related events from RabbitMQ and exposes typed callbacks.
/// </summary>
public class SettingsEventConsumer : BaseRabbitMQConsumer
{
    public override string ServiceName => "RabbitMQ Settings Event Consumer";
    private const string QueueName = "lightview.camera-controller.settings";

    public event Func<CameraMonitoringSettingsUpdatedEvent, Task>? CameraMonitoringSettingsUpdated;

    public SettingsEventConsumer(
        ILogger<SettingsEventConsumer> logger,
        IOptions<RabbitMQConfiguration> config)
        : base(logger, config)
    {}

    protected override async Task OnInitializedAsync(CancellationToken cancellationToken = default)
    {
        // Declare settings exchange
        await _channel!.ExchangeDeclareAsync(
            exchange: ExchangesConfiguration.SettingsEventsExchange,
            type: ExchangesConfiguration.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null);

        // Declare and bind queue dedicated for camera-controller settings
        await DeclareQueueAsync(QueueName, durable: true);
        await BindQueue(QueueName, ExchangesConfiguration.SettingsEventsExchange, SettingsEventRoutingKeys.CameraMonitoringUpdated);

        // Start consuming with prefetch=1
        await StartConsumingAsync(QueueName, autoAck: false, prefetchCount: 1);
    }

    protected override async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        var data = eventArgs.Body.ToArray();
        var routingKey = eventArgs.RoutingKey;

        if (routingKey == SettingsEventRoutingKeys.CameraMonitoringUpdated)
        {
            await HandleTypedEventAsync<CameraMonitoringSettingsUpdatedEvent>(data, CameraMonitoringSettingsUpdated);
        }
        else
        {
            _logger.LogWarning("Unknown settings routing key: {RoutingKey}", routingKey);
        }
    }

    private async Task HandleTypedEventAsync<T>(byte[] data, Func<T, Task>? handler)
        where T : class
    {
        try
        {
            var message = DeserializeMessage<T>(data);
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize settings event to {Type}", typeof(T).Name);
                return;
            }
            if (handler != null)
            {
                await handler(message);
            }
            else
            {
                _logger.LogDebug("No handler registered for {Type}", typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling settings event {Type}", typeof(T).Name);
            throw;
        }
    }
}