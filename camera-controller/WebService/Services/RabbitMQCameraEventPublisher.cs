using System.Text;
using System.Text.Json;
using Lightview.Shared.Contracts.Configuration;
using Lightview.Shared.Contracts.Events;
using Lightview.Shared.Contracts.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace WebService.Services;

/// <summary>
/// RabbitMQ-based implementation of camera event publisher
/// </summary>
public class RabbitMQCameraEventPublisher : ICameraEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMQCameraEventPublisher> _logger;
    private readonly RabbitMQConfiguration _config;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;
    private readonly object _connectionLock = new();
    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);
    private readonly int _maxStartupRetries = 5;
    private readonly TimeSpan _startupRetryDelay = TimeSpan.FromSeconds(1);

    public RabbitMQCameraEventPublisher(
        ILogger<RabbitMQCameraEventPublisher> logger,
        IOptions<RabbitMQConfiguration> config,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _config = config.Value;
        _applicationLifetime = applicationLifetime;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Ensure connection on startup - exit app if cannot connect
        _ = Task.Run(EnsureInitialConnectionAsync);
    }

    public async Task PublishCameraStatusChangedAsync(CameraStatusChangedEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.StatusChanged, cancellationToken);
    }

    public async Task PublishCameraErrorAsync(CameraErrorEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.Error, cancellationToken);
    }

    public async Task PublishPtzMovedAsync(PtzMovedEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.PtzMoved, cancellationToken);
    }

    public async Task PublishCameraStatisticsAsync(CameraStatisticsEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.CameraStatistics, cancellationToken);
    }

    public async Task PublishCameraMetadataUpdatedAsync(CameraMetadataUpdatedEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing camera metadata profiles: {Profiles}", 
            cameraEvent.Profiles != null ? string.Join(", ", cameraEvent.Profiles.Select(p => p.WebRtcUri?.ToString() ?? "null")) : "null");
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.MetadataUpdated, cancellationToken);
    }

    public async Task PublishCameraEventAsync<T>(T cameraEvent, CancellationToken cancellationToken = default) where T : CameraEventBase
    {
        var routingKey = GetRoutingKeyForEventType<T>();
        await PublishCameraEventAsync(cameraEvent, routingKey, cancellationToken);
    }

    private async Task EnsureInitialConnectionAsync()
    {
        _logger.LogInformation("Starting RabbitMQ camera event publisher - attempting initial connection");
        
        var retryCount = 0;
        while (retryCount < _maxStartupRetries)
        {
            try
            {
                retryCount++;
                _logger.LogInformation("Attempting to connect to RabbitMQ (attempt {Attempt}/{MaxAttempts})", 
                    retryCount, _maxStartupRetries);
                
                if (TryConnect())
                {
                    _logger.LogInformation("Successfully connected to RabbitMQ on startup");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ on startup attempt {Attempt}", retryCount);
            }
            
            if (retryCount < _maxStartupRetries)
            {
                _logger.LogInformation("Retrying RabbitMQ connection in {Delay} seconds", _startupRetryDelay.TotalSeconds);
                await Task.Delay(_startupRetryDelay);
            }
        }
        
        _logger.LogCritical("Failed to connect to RabbitMQ after {MaxAttempts} attempts. Camera-controller cannot function without message broker. Shutting down application.", 
            _maxStartupRetries);
        _applicationLifetime.StopApplication();
    }

    private void EnsureConnection()
    {
        lock (_connectionLock)
        {
            // Check if we should attempt reconnection
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                return;

            // Rate limit reconnection attempts
            if (DateTime.UtcNow - _lastReconnectAttempt < _reconnectInterval)
                return;

            _lastReconnectAttempt = DateTime.UtcNow;
            TryConnect();
        }
    }

    private bool TryConnect()
    {
        try
        {
            // Dispose existing connections
            _channel?.Dispose();
            _connection?.Dispose();

            // Create new RabbitMQ connection
            var factory = new ConnectionFactory
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.Username,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(_config.ConnectionTimeoutMs),
                NetworkRecoveryInterval = TimeSpan.FromMilliseconds(_config.NetworkRecoveryInterval),
                AutomaticRecoveryEnabled = _config.AutomaticRecoveryEnabled,
                RequestedHeartbeat = TimeSpan.FromSeconds(_config.RequestedHeartbeat)
            };

            _connection = factory.CreateConnection("CameraController");
            _channel = _connection.CreateModel();

            // Declare the exchange
            _channel.ExchangeDeclare(
                exchange: _config.EventsExchange,
                type: _config.EventsExchangeType,
                durable: true,
                autoDelete: false
            );

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}, Exchange: {Exchange}", 
                _config.Host, _config.Port, _config.EventsExchange);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}", 
                _config.Host, _config.Port);
            
            _connection?.Dispose();
            _channel?.Dispose();
            _connection = null;
            _channel = null;
            
            return false;
        }
    }

    private async Task PublishCameraEventAsync<T>(T cameraEvent, string routingKey, CancellationToken cancellationToken = default) where T : CameraEventBase
    {
        // Ensure we have a valid connection
        EnsureConnection();
        
        if (_channel == null || _connection == null)
        {
            _logger.LogError("Cannot publish event - RabbitMQ connection not available after reconnection attempt");
            return;
        }

        var maxRetries = 2;
        var retryCount = 0;
        
        while (retryCount <= maxRetries)
        {
            try
            {
                var json = JsonSerializer.Serialize(cameraEvent, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Type = cameraEvent.EventType;
                properties.Headers = new Dictionary<string, object?>
                {
                    ["CameraId"] = cameraEvent.CameraId.ToString(),
                    ["EventType"] = cameraEvent.EventType,
                    ["Source"] = "CameraController",
                    ["PublishedAt"] = cameraEvent.Timestamp.ToString("O")
                };

                _channel.BasicPublish(
                    exchange: _config.EventsExchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                await Task.CompletedTask;

                _logger.LogDebug("Published {EventType} event for camera {CameraId} with routing key {RoutingKey}", 
                    cameraEvent.EventType, cameraEvent.CameraId, routingKey);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "Failed to publish {EventType} event for camera {CameraId}, attempt {Attempt}/{MaxRetries}", 
                    cameraEvent.EventType, cameraEvent.CameraId, retryCount, maxRetries + 1);
                
                // Try to reconnect for next attempt
                EnsureConnection();
                
                if (_channel == null)
                {
                    _logger.LogError("Cannot retry publish - RabbitMQ reconnection failed");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish {EventType} event for camera {CameraId} after {Attempts} attempts", 
                    cameraEvent.EventType, cameraEvent.CameraId, maxRetries + 1);
                throw;
            }
        }
    }

    private string GetRoutingKeyForEventType<T>() where T : CameraEventBase
    {
        return typeof(T).Name switch
        {
            nameof(CameraStatusChangedEvent) => CameraEventRoutingKeys.StatusChanged,
            nameof(CameraErrorEvent) => CameraEventRoutingKeys.Error,
            nameof(PtzMovedEvent) => CameraEventRoutingKeys.PtzMoved,
            nameof(CameraStatisticsEvent) => CameraEventRoutingKeys.CameraStatistics,
            nameof(CameraMetadataUpdatedEvent) => CameraEventRoutingKeys.MetadataUpdated,
            _ => "camera.unknown"
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_connectionLock)
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                
                _logger.LogInformation("RabbitMQ connection closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ connection");
            }

            _disposed = true;
        }
    }
}