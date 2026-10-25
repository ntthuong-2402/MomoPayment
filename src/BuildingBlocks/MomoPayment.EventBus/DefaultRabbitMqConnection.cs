using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MomoPayment.EventBus;

public interface IRabbitMqConnection : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    IModel CreateModel();
}

public class DefaultRabbitMqConnection : IRabbitMqConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<DefaultRabbitMqConnection> _logger;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private bool _disposed;
    private readonly object _syncRoot = new();

    public DefaultRabbitMqConnection(
        IConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<DefaultRabbitMqConnection> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _options = options.Value;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            TryConnect();
        }

        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection!.CreateModel();
    }

    public bool TryConnect()
    {
        lock (_syncRoot)
        {
            if (IsConnected) return true;

            _logger.LogInformation("RabbitMQ Client is trying to connect to {Host}:{Port}...", _options.HostName, _options.Port);

            for (int i = 0; i < _options.RetryCount; i++)
            {
                try
                {
                    _connection = _connectionFactory.CreateConnection();
                    if (IsConnected)
                    {
                        _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}'", _options.HostName);
                        return true;
                    }
                }
                catch (BrokerUnreachableException ex)
                {
                    _logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt} failed", i + 1);
                    Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error connecting to RabbitMQ on attempt {Attempt}", i + 1);
                    Thread.Sleep(2000);
                }
            }

            _logger.LogError("FATAL ERROR: RabbitMQ connections could not be created and opened after retries");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex, "Failed to cleanly dispose RabbitMQ connection");
        }
    }
}
