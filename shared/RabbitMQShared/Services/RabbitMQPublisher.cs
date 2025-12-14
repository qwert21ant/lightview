using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQShared.Configuration;

namespace RabbitMQShared.Services;

/// <summary>
/// Generic RabbitMQ publisher service for publishing messages to exchanges
/// </summary>
public class RabbitMQPublisher : BaseRabbitMQService
{
    public override string ServiceName => "RabbitMQ-Publisher";
    
    private readonly JsonSerializerOptions _jsonOptions;
    
    public RabbitMQPublisher(
        ILogger<RabbitMQPublisher> logger,
        IOptions<RabbitMQConfiguration> config) : base(logger, config)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    /// <summary>
    /// Declare an exchange if it doesn't exist
    /// </summary>
    public async Task DeclareExchangeAsync(string exchangeName, string exchangeType = ExchangeType.Topic, bool durable = true)
    {
        EnsureConnected();
        await _channel!.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: durable);
        _logger.LogDebug("Declared exchange: {ExchangeName} (Type: {ExchangeType})", exchangeName, exchangeType);
    }
    
    /// <summary>
    /// Publish a message to an exchange with a routing key
    /// </summary>
    public async Task PublishMessageAsync<T>(string exchangeName, string routingKey, T message, bool persistent = true) where T : class
    {
        EnsureConnected();
        
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);
        
        var properties = new BasicProperties();
        properties.Persistent = persistent;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.MessageId = Guid.NewGuid().ToString();
        properties.ContentType = "application/json";
        properties.ContentEncoding = "utf-8";
        
        await _channel!.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
        
        _logger.LogDebug("Published message to exchange: {ExchangeName}, routing key: {RoutingKey}, message ID: {MessageId}", 
            exchangeName, routingKey, properties.MessageId);
    }
}