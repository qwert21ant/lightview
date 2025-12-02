using Lightview.Shared.Contracts.Interfaces;

namespace WebService.Services;

/// <summary>
/// Background service that manages the RabbitMQ camera event consumer lifecycle
/// </summary>
public class CameraEventConsumerService : BackgroundService
{
    private readonly ICameraEventConsumer _eventConsumer;
    private readonly ILogger<CameraEventConsumerService> _logger;

    public CameraEventConsumerService(
        ICameraEventConsumer eventConsumer,
        ILogger<CameraEventConsumerService> logger)
    {
        _eventConsumer = eventConsumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting camera event consumer service");
            
            // Start consuming events
            await _eventConsumer.StartConsumingAsync(stoppingToken);
            
            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("Camera event consumer service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in camera event consumer service");
            throw;
        }
        finally
        {
            try
            {
                await _eventConsumer.StopConsumingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping camera event consumer");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping camera event consumer service");
        
        try
        {
            await _eventConsumer.StopConsumingAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping event consumer during service shutdown");
        }

        await base.StopAsync(cancellationToken);
    }
}