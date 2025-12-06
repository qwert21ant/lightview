using System.Text;
using System.Text.Json;
using Lightview.Shared.Contracts.Configuration;
using Lightview.Shared.Contracts.Events;
using Lightview.Shared.Contracts.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebService.Services;

/// <summary>
/// RabbitMQ-based implementation of camera event consumer for core service
/// </summary>
public class RabbitMQCameraEventConsumer : ICameraEventConsumer, IDisposable
{
    private readonly ILogger<RabbitMQCameraEventConsumer> _logger;
    private readonly RabbitMQConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IHostApplicationLifetime _applicationLifetime;
    
    private IConnection? _connection;
    private IModel? _channel;
    private string? _consumerTag;
    
    private readonly object _connectionLock = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed;
    private bool _isConsuming;
    
    // Reconnection policy
    private int _reconnectAttempts;
    private readonly int _maxReconnectAttempts = 10;
    private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(1);
    private DateTime _lastReconnectAttempt = DateTime.MinValue;

    // Events - using Func<T, Task> for async event handlers
    public event Func<CameraStatusChangedEvent, Task>? CameraStatusChanged;
    public event Func<CameraErrorEvent, Task>? CameraError;
    public event Func<PtzMovedEvent, Task>? PtzMoved;
    public event Func<CameraStatisticsEvent, Task>? CameraStatistics;
    public event Func<CameraMetadataUpdatedEvent, Task>? CameraMetadataUpdated;

    public RabbitMQCameraEventConsumer(
        ILogger<RabbitMQCameraEventConsumer> logger,
        IOptions<RabbitMQConfiguration> config,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _config = config.Value;
        _applicationLifetime = applicationLifetime;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        if (_isConsuming)
        {
            _logger.LogWarning("Consumer is already running");
            return;
        }

        _logger.LogInformation("Starting RabbitMQ camera event consumer");
        
        if (!await TryConnect())
        {
            _logger.LogCritical("Failed to connect to RabbitMQ after {MaxAttempts} attempts. Shutting down application.", 
                _maxReconnectAttempts);
            _applicationLifetime.StopApplication();
            return;
        }

        try
        {
            lock (_connectionLock)
            {
                if (_channel == null)
                {
                    _logger.LogError("Cannot start consuming - channel is null");
                    return;
                }

                // Declare single queue with binding to exchange
                _channel.QueueDeclare(
                    queue: _config.CoreQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                _channel.QueueBind(
                    queue: _config.CoreQueueName,
                    exchange: _config.EventsExchange,
                    routingKey: _config.CoreRoutingKey
                );

                // Set prefetch count of 1 for sequential processing
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageReceivedAsync;
                consumer.ConsumerCancelled += async (sender, e) => { OnConsumerCancelled(sender, e); await Task.CompletedTask; };
                consumer.Shutdown += async (sender, e) => { OnConsumerShutdown(sender, e); await Task.CompletedTask; };

                // Start consuming
                _consumerTag = _channel.BasicConsume(
                    queue: _config.CoreQueueName,
                    autoAck: false, // Manual acknowledgment for reliability
                    consumer: consumer
                );

                _isConsuming = true;
                _logger.LogInformation("Started consuming camera events from queue {Queue}", _config.CoreQueueName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming camera events");
            throw;
        }

        // Start background reconnection monitoring
        _ = Task.Run(MonitorConnection, cancellationToken);
    }

    public async Task StopConsumingAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConsuming)
            return;

        _logger.LogInformation("Stopping RabbitMQ camera event consumer");
        
        try
        {
            _cancellationTokenSource.Cancel();

            lock (_connectionLock)
            {
                if (_channel != null && !string.IsNullOrEmpty(_consumerTag))
                {
                    _channel.BasicCancel(_consumerTag);
                    _consumerTag = null;
                }

                _isConsuming = false;
            }

            _logger.LogInformation("Stopped consuming camera events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping consumer");
        }

        await Task.CompletedTask;
    }

    private async Task<bool> TryConnect()
    {
        while (_reconnectAttempts < _maxReconnectAttempts && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                _reconnectAttempts++;
                _lastReconnectAttempt = DateTime.UtcNow;

                _logger.LogInformation("Attempting to connect to RabbitMQ (attempt {Attempt}/{MaxAttempts})", 
                    _reconnectAttempts, _maxReconnectAttempts);

                lock (_connectionLock)
                {
                    // Dispose existing connections
                    _channel?.Dispose();
                    _connection?.Dispose();

                    // Create new connection
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
                        RequestedHeartbeat = TimeSpan.FromSeconds(_config.RequestedHeartbeat),
                        DispatchConsumersAsync = true
                    };

                    _connection = factory.CreateConnection("CoreEventConsumer");
                    _channel = _connection.CreateModel();

                    // Declare exchange if it doesn't exist
                    _channel.ExchangeDeclare(
                        exchange: _config.EventsExchange,
                        type: _config.EventsExchangeType,
                        durable: true,
                        autoDelete: false
                    );
                }

                _logger.LogInformation("Successfully connected to RabbitMQ at {Host}:{Port}", 
                    _config.Host, _config.Port);
                
                _reconnectAttempts = 0; // Reset counter on successful connection
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxAttempts})", 
                    _reconnectAttempts, _maxReconnectAttempts);

                if (_reconnectAttempts < _maxReconnectAttempts)
                {
                    _logger.LogInformation("Retrying connection in {Delay} seconds", _reconnectDelay.TotalSeconds);
                    await Task.Delay(_reconnectDelay, _cancellationTokenSource.Token);
                }
            }
        }

        return false;
    }

    private async Task MonitorConnection()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), _cancellationTokenSource.Token);

                // Check connection health
                bool needsReconnect = false;
                lock (_connectionLock)
                {
                    if (_connection == null || !_connection.IsOpen || _channel == null || !_channel.IsOpen)
                    {
                        if (_isConsuming)
                        {
                            _logger.LogWarning("RabbitMQ connection lost, attempting to reconnect");
                            _isConsuming = false;
                            needsReconnect = true;
                        }
                    }
                }

                if (needsReconnect)
                {
                    if (!await TryConnect())
                    {
                        _logger.LogCritical("Failed to reconnect to RabbitMQ. Shutting down application.");
                        _applicationLifetime.StopApplication();
                        return;
                    }

                    // Restart consuming
                    await StartConsumingAsync(_cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connection monitoring");
            }
        }
    }

    private async Task OnMessageReceivedAsync(object? sender, BasicDeliverEventArgs e)
    {
        try
        {
            var json = Encoding.UTF8.GetString(e.Body.Span);
            var eventType = e.BasicProperties.Type;

            _logger.LogDebug("Received camera event: {EventType}", eventType);

            // Process the event based on type from message properties
            var handled = await HandleEventAsync(json, eventType);

            if (handled)
            {
                // Acknowledge message only after successful processing
                AcknowledgeMessage(e.DeliveryTag, true);
            }
            else
            {
                _logger.LogWarning("Unknown event type: {EventType}", eventType);
                // Reject unknown message types without requeue
                AcknowledgeMessage(e.DeliveryTag, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera event");
            
            // Negative acknowledgment - requeue for retry
            NegativeAcknowledgeMessage(e.DeliveryTag);
        }
    }

    private async Task<bool> HandleEventAsync(string json, string eventType)
    {
        try
        {
            return eventType switch
            {
                nameof(CameraStatusChangedEvent) => await HandleEventAsync(json, CameraStatusChanged),
                nameof(CameraErrorEvent) => await HandleEventAsync(json, CameraError),
                nameof(PtzMovedEvent) => await HandleEventAsync(json, PtzMoved),
                nameof(CameraStatisticsEvent) => await HandleEventAsync(json, CameraStatistics),
                nameof(CameraMetadataUpdatedEvent) => await HandleEventAsync(json, CameraMetadataUpdated),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in event handler for type {EventType}", eventType);
            return false;
        }
    }

    private async Task<bool> HandleEventAsync<T>(string json, Func<T, Task?>? handler) where T : CameraEventBase
    {
        try
        {
            var eventData = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            if (eventData != null && handler != null)
            {
                var task = handler(eventData);
                if (task != null)
                {
                    await task;
                }
                return true;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize event of type {EventType}", typeof(T).Name);
        }

        return false;
    }

    private void AcknowledgeMessage(ulong deliveryTag, bool ack)
    {
        try
        {
            lock (_connectionLock)
            {
                if (ack)
                    _channel?.BasicAck(deliveryTag, false);
                else
                    _channel?.BasicNack(deliveryTag, false, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging message");
        }
    }

    private void NegativeAcknowledgeMessage(ulong deliveryTag)
    {
        try
        {
            lock (_connectionLock)
            {
                _channel?.BasicNack(deliveryTag, false, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error negative acknowledging message");
        }
    }

    private void OnConsumerCancelled(object? sender, ConsumerEventArgs e)
    {
        _logger.LogWarning("Consumer was cancelled: {ConsumerTag}", e.ConsumerTags?.FirstOrDefault());
        _isConsuming = false;
    }

    private void OnConsumerShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("Consumer shutdown: {Reason}", e.ReplyText);
        _isConsuming = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _cancellationTokenSource.Cancel();

            lock (_connectionLock)
            {
                if (_channel != null && !string.IsNullOrEmpty(_consumerTag))
                {
                    _channel.BasicCancel(_consumerTag);
                }

                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }

            _cancellationTokenSource.Dispose();
            _logger.LogInformation("RabbitMQ event consumer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ event consumer");
        }

        _disposed = true;
    }
}