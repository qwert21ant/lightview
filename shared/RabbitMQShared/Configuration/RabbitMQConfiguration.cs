namespace RabbitMQShared.Configuration;

/// <summary>
/// RabbitMQ connection configuration
/// </summary>
public class RabbitMQConfiguration
{
    public const string SectionName = "RabbitMQ";
    
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    // Connection settings
    public int ConnectionTimeout { get; set; } = 30;
    public int NetworkRecoveryInterval { get; set; } = 5;
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    // Retry settings
    public int RetryAttempts { get; set; } = 5;
    public int RetryDelay { get; set; } = 3;
}
