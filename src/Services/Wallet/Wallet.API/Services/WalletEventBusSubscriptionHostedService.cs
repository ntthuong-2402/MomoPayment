using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MomoPayment.EventBus;
using MomoPayment.EventBus.Events;
using Wallet.API.Consumers;

namespace Wallet.API.Services;

public class WalletEventBusSubscriptionHostedService : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<WalletEventBusSubscriptionHostedService> _logger;

    public WalletEventBusSubscriptionHostedService(IEventBus eventBus, ILogger<WalletEventBusSubscriptionHostedService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting RabbitMQ subscriptions for Wallet Service...");

        try
        {
            _eventBus.Subscribe<UserRegisteredEvent, UserRegisteredConsumer>();
            _logger.LogInformation("Subscribed to UserRegisteredEvent successfully in Wallet Service");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start RabbitMQ subscriptions in Wallet Service");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
