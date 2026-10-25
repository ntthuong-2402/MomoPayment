using Microsoft.Extensions.Logging;
using MomoPayment.EventBus;
using MomoPayment.EventBus.Events;
using Wallet.API.Services;

namespace Wallet.API.Consumers;

public class UserRegisteredConsumer : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(IWalletService walletService, ILogger<UserRegisteredConsumer> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent @event)
    {
        _logger.LogInformation("Processing UserRegisteredEvent for new user: {UserId}, Phone: {Phone}", @event.UserId, @event.PhoneNumber);

        try
        {
            var wallet = await _walletService.GetOrCreateWalletAsync(@event.UserId, @event.PhoneNumber);
            _logger.LogInformation("Wallet successfully provisioned for user: {UserId}, WalletId: {WalletId}", @event.UserId, wallet.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to automatically provision wallet for user: {UserId}", @event.UserId);
        }
    }
}
