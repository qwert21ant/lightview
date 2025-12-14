using System.Text;
using Lightview.Shared.Contracts.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using RabbitMQShared.Services;
using RabbitMQShared.Configuration;

namespace WebService.Services.Events;

/// <summary>
/// RabbitMQ-based implementation of camera event consumer for core service using shared infrastructure
/// </summary>
public class CameraEventConsumer : BaseRabbitMQConsumer
{
    public override string ServiceName => "RabbitMQ Camera Event Consumer";
    public const string QueueName = "lightview.core.camera";
    public const string RoutingKey = "camera.#";

    // Events - using Func<T, Task> for async event handlers
    public event Func<CameraStatusChangedEvent, Task>? CameraStatusChanged;
    public event Func<CameraErrorEvent, Task>? CameraError;
    public event Func<PtzMovedEvent, Task>? PtzMoved;
    public event Func<CameraStatisticsEvent, Task>? CameraStatistics;
    public event Func<CameraMetadataUpdatedEvent, Task>? CameraMetadataUpdated;
    public event Func<CameraSnapshotCapturedEvent, Task>? CameraSnapshotCaptured;

    public CameraEventConsumer(
        ILogger<CameraEventConsumer> logger,
        IOptions<RabbitMQConfiguration> config)
        : base(logger, config)
    {}

    protected override async Task OnInitializedAsync(CancellationToken cancellationToken = default)
    {
        // Declare exchange
        await _channel!.ExchangeDeclareAsync(
            exchange: ExchangesConfiguration.CameraEventsExchange,
            type: ExchangesConfiguration.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null);
        
        // Declare and bind queue
        await DeclareQueueAsync(QueueName, durable: true);
        await BindQueue(QueueName, ExchangesConfiguration.CameraEventsExchange, RoutingKey);
        
        // Start consuming
        await StartConsumingAsync(QueueName, autoAck: false, prefetchCount: 1);
    }

    protected override async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var routingKey = eventArgs.RoutingKey;

            _logger.LogDebug("Received camera event with routing key: {RoutingKey}", routingKey);

            // Process the event based on routing key
            await HandleCameraEventAsync(json, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera event with routing key {RoutingKey}", eventArgs.RoutingKey);
            throw; // Re-throw to let base consumer handle retry logic
        }
    }

    private async Task HandleCameraEventAsync(string json, string routingKey)
    {
        switch (routingKey)
        {
            case CameraEventRoutingKeys.StatusChanged:
                await HandleTypedEventAsync<CameraStatusChangedEvent>(json, CameraStatusChanged);
                break;
            case CameraEventRoutingKeys.Error:
                await HandleTypedEventAsync<CameraErrorEvent>(json, CameraError);
                break;
            case CameraEventRoutingKeys.PtzMoved:
                await HandleTypedEventAsync<PtzMovedEvent>(json, PtzMoved);
                break;
            case CameraEventRoutingKeys.CameraStatistics:
                await HandleTypedEventAsync<CameraStatisticsEvent>(json, CameraStatistics);
                break;
            case CameraEventRoutingKeys.MetadataUpdated:
                await HandleTypedEventAsync<CameraMetadataUpdatedEvent>(json, CameraMetadataUpdated);
                break;
            case CameraEventRoutingKeys.SnapshotCaptured:
                await HandleTypedEventAsync<CameraSnapshotCapturedEvent>(json, CameraSnapshotCaptured);
                break;
            default:
                _logger.LogWarning("Unknown camera event routing key: {RoutingKey}", routingKey);
                break;
        }
    }

    private async Task HandleTypedEventAsync<T>(string json, Func<T, Task>? handler) where T : CameraEventBase
    {
        if (handler == null)
            return;

        var eventData = DeserializeMessage<T>(Encoding.UTF8.GetBytes(json));
        if (eventData != null)
        {
            await handler(eventData);
        }
    }
}