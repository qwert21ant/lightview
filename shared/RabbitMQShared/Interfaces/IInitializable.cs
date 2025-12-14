namespace RabbitMQShared.Interfaces;

/// <summary>
/// Interface for services that must be initialized before the application can start
/// </summary>
public interface IInitializable
{
    /// <summary>
    /// Initialize the service. This method must complete successfully before the application starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization operation</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Name of the service for logging purposes
    /// </summary>
    string ServiceName { get; }
}