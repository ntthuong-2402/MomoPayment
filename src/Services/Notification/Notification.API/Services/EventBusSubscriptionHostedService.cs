using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MomoPayment.EventBus;
using MomoPayment.EventBus.Events;
using Notification.API.Consumers;

namespace Notification.API.Services;

public class EventBusSubscriptionHostedService : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventBusSubscriptionHostedService> _logger;

    public EventBusSubscriptionHostedService(IEventBus eventBus, ILogger<EventBusSubscriptionHostedService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting RabbitMQ subscriptions for Notification Service...");

        try
        {
            _eventBus.Subscribe<TransactionCompletedEvent, TransactionCompletedConsumer>();
            _eventBus.Subscribe<WalletBalanceChangedEvent, WalletBalanceChangedConsumer>();
            _eventBus.Subscribe<UserRegisteredEvent, UserRegisteredConsumer>();
            _logger.LogInformation("Subscribed to TransactionCompletedEvent, WalletBalanceChangedEvent, and UserRegisteredEvent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start RabbitMQ subscriptions. Message consumer will retry on connection");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
