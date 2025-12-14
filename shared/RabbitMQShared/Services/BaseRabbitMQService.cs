using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQShared.Configuration;
using RabbitMQShared.Interfaces;

namespace RabbitMQShared.Services;

/// <summary>
/// Base service for RabbitMQ connection management with automatic recovery
/// </summary>
public abstract class BaseRabbitMQService : IInitializable, IAsyncDisposable
{
    protected readonly ILogger _logger;
    protected readonly RabbitMQConfiguration _config;
    protected IConnection? _connection;
    protected IChannel? _channel;
    protected readonly object _connectionLock = new();
    protected bool _disposed = false;
    
    public abstract string ServiceName { get; }
    
    protected BaseRabbitMQService(
        ILogger logger,
        IOptions<RabbitMQConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }
    
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await ConnectWithRetryAsync(cancellationToken);
        await OnInitializedAsync(cancellationToken);
    }
    
    /// <summary>
    /// Called after successful connection establishment
    /// </summary>
    protected virtual Task OnInitializedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    protected async Task ConnectWithRetryAsync(CancellationToken cancellationToken = default)
    {
        var maxRetries = _config.RetryAttempts;
        var retryDelay = TimeSpan.FromSeconds(_config.RetryDelay);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("{ServiceName}: Attempting RabbitMQ connection (attempt {Attempt}/{MaxRetries})", 
                    ServiceName, attempt, maxRetries);
                
                await ConnectAsync(cancellationToken);
                
                _logger.LogInformation("{ServiceName}: Successfully connected to RabbitMQ at {Host}:{Port}", 
                    ServiceName, _config.Host, _config.Port);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "{ServiceName}: Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries}). Retrying in {RetryDelay}s...", 
                    ServiceName, attempt, maxRetries, retryDelay.TotalSeconds);
                
                await Task.Delay(retryDelay, cancellationToken);
            }
        }
        
        throw new InvalidOperationException($"{ServiceName}: Failed to connect to RabbitMQ after {maxRetries} attempts");
    }
    
    protected virtual async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connection?.IsOpen == true)
            return;
        
        var factory = new ConnectionFactory
        {
            HostName = _config.Host,
            Port = _config.Port,
            UserName = _config.Username,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            AutomaticRecoveryEnabled = _config.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_config.NetworkRecoveryInterval),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(_config.ConnectionTimeout)
        };
        
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        // Setup connection recovery events
        _connection.ConnectionShutdownAsync += OnConnectionShutdown;
    }
    
    protected virtual async Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (!_disposed)
        {
            _logger.LogWarning("{ServiceName}: RabbitMQ connection shutdown: {Reason}", ServiceName, e.ReplyText);
        }
    }
    

    
    protected void EnsureConnected()
    {
        if (_connection?.IsOpen != true || _channel?.IsOpen != true)
        {
            throw new InvalidOperationException($"{ServiceName}: RabbitMQ connection is not available. Service may not be properly initialized.");
        }
    }
    
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        try
        {
            await _channel?.CloseAsync();
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ServiceName}: Error closing RabbitMQ channel", ServiceName);
        }
        
        try
        {
            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                
                await _connection.CloseAsync();
                _connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ServiceName}: Error closing RabbitMQ connection", ServiceName);
        }
        
        _logger.LogDebug("{ServiceName}: RabbitMQ service disposed", ServiceName);
    }
}