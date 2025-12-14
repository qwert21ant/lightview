using Lightview.Shared.Contracts.Events;
using Lightview.Shared.Contracts.Settings;
using RabbitMQShared.Services;
using RabbitMQShared.Configuration;

namespace WebService.Services.Events;

/// <summary>
/// Publishes events when core settings are updated.
/// </summary>
public class SettingsEventPublisher
{
    private readonly RabbitMQPublisher _publisher;
    private readonly ILogger<SettingsEventPublisher> _logger;

    public SettingsEventPublisher(RabbitMQPublisher publisher, ILogger<SettingsEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishCameraMonitoringSettingsUpdatedAsync(CameraMonitoringSettings settings, CancellationToken cancellationToken = default)
    {
        await _publisher.DeclareExchangeAsync(ExchangesConfiguration.SettingsEventsExchange, ExchangesConfiguration.ExchangeType, durable: true);
        var evt = new CameraMonitoringSettingsUpdatedEvent
        {
            Settings = settings
        };
        await _publisher.PublishMessageAsync(ExchangesConfiguration.SettingsEventsExchange, SettingsEventRoutingKeys.CameraMonitoringUpdated, evt);
        _logger.LogInformation("Published CameraMonitoringSettingsUpdatedEvent");
    }
}
