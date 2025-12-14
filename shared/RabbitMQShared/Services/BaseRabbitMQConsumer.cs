using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQShared.Configuration;

namespace RabbitMQShared.Services;

/// <summary>
/// Generic RabbitMQ consumer service for consuming messages from queues
/// </summary>
public abstract class BaseRabbitMQConsumer : BaseRabbitMQService
{
    protected readonly JsonSerializerOptions _jsonOptions;
    protected AsyncEventingBasicConsumer? _consumer;
    protected string? _consumerTag;
    
    public override string ServiceName => GetType().Name;
    
    protected BaseRabbitMQConsumer(
        ILogger logger,
        IOptions<RabbitMQConfiguration> config) : base(logger, config)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
    
    /// <summary>
    /// Declare a queue with the specified configuration
    /// </summary>
    protected async Task DeclareQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object?>? arguments = null)
    {
        EnsureConnected();
        await _channel!.QueueDeclareAsync(queue: queueName, durable: durable, exclusive: exclusive, autoDelete: autoDelete, arguments: arguments);
        _logger.LogDebug("Declared queue: {QueueName}", queueName);
    }
    
    /// <summary>
    /// Bind a queue to an exchange with a routing key
    /// </summary>
    protected async Task BindQueue(string queueName, string exchangeName, string routingKey = "")
    {
        EnsureConnected();
        await _channel!.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: routingKey);
        _logger.LogDebug("Bound queue {QueueName} to exchange {ExchangeName} with routing key: {RoutingKey}", 
            queueName, exchangeName, routingKey);
    }
    
    /// <summary>
    /// Start consuming messages from a queue
    /// </summary>
    protected async Task StartConsumingAsync(string queueName, bool autoAck = false, ushort prefetchCount = 1)
    {
        EnsureConnected();
        
        // Set QoS to control message prefetch
        await _channel!.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetchCount, global: false);
        
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                await HandleMessageAsync(ea);
                
                if (!autoAck)
                {
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}. Message ID: {MessageId}", 
                    queueName, ea.BasicProperties?.MessageId);
                
                if (!autoAck)
                {
                    // Reject and requeue the message
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
        };
        
        _consumerTag = await _channel.BasicConsumeAsync(queue: queueName, autoAck: autoAck, consumer: _consumer);
        _logger.LogInformation("Started consuming from queue: {QueueName} (Consumer tag: {ConsumerTag})", 
            queueName, _consumerTag);
    }

    /// <summary>
    /// Handle incoming message - must be implemented by derived classes
    /// </summary>
    protected abstract Task HandleMessageAsync(BasicDeliverEventArgs eventArgs);
 
    /// <summary>
    /// Deserialize message body to specified type
    /// </summary>
    protected T? DeserializeMessage<T>(byte[] body) where T : class
    {
        try
        {
            var json = Encoding.UTF8.GetString(body);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message to type {Type}", typeof(T).Name);
            return null;
        }
    }

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    protected async Task StopConsumingAsync()
    {
        if (_consumerTag != null && _channel?.IsOpen == true)
        {
            try
            {
                await _channel.BasicCancelAsync(_consumerTag);
                _logger.LogInformation("Stopped consuming (Consumer tag: {ConsumerTag})", _consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping consumer: {ConsumerTag}", _consumerTag);
            }
        }
        
        _consumerTag = null;
        _consumer = null;
    }
    
    public override async ValueTask DisposeAsync()
    {
        await StopConsumingAsync();
        await base.DisposeAsync();
    }
}