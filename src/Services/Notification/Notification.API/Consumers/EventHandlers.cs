using Microsoft.Extensions.Logging;
using MomoPayment.Common.Enums;
using MomoPayment.EventBus;
using MomoPayment.EventBus.Events;
using Notification.API.Data;
using Notification.API.Entities;

namespace Notification.API.Consumers;

public class TransactionCompletedConsumer : IIntegrationEventHandler<TransactionCompletedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<TransactionCompletedConsumer> _logger;

    public TransactionCompletedConsumer(
        NotificationDbContext dbContext,
        ILogger<TransactionCompletedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(TransactionCompletedEvent @event)
    {
        _logger.LogInformation("Processing TransactionCompletedEvent for txn: {TxnCode}", @event.TransactionCode);

        var notifications = new List<AppNotification>();

        // 1. Notification for Sender (Tiền chuyển đi hoặc Thanh toán đơn hàng)
        if (@event.SenderUserId.HasValue)
        {
            var isPayment = @event.Type.Equals("Payment", StringComparison.OrdinalIgnoreCase);
            var title = isPayment ? "Biến động số dư: Thanh toán thành công" : "Biến động số dư: Chuyển tiền thành công";
            var content = isPayment
                ? $"Bạn đã thanh toán thành công {@event.Amount:N0} VND. {@event.Description}"
                : $"Bạn đã chuyển thành công {@event.Amount:N0} VND đến {@event.ReceiverPhone}. Lời nhắn: {@event.Description}";
            var notifType = isPayment ? NotificationType.PaymentSuccess : NotificationType.MoneySent;

            var senderNotif = new AppNotification
            {
                Id = Guid.NewGuid(),
                UserId = @event.SenderUserId.Value,
                Title = title,
                Content = content,
                Type = notifType,
                ReferenceTransactionCode = @event.TransactionCode,
                Amount = @event.Amount,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            notifications.Add(senderNotif);
        }

        // 2. Notification for Receiver (Tiền nhận được)
        if (@event.ReceiverUserId.HasValue)
        {
            var receiverNotif = new AppNotification
            {
                Id = Guid.NewGuid(),
                UserId = @event.ReceiverUserId.Value,
                Title = "Biến động số dư: Nhận tiền MoMo",
                Content = $"Bạn vừa nhận được +{@event.Amount:N0} VND từ {@event.SenderPhone}. Lời nhắn: {@event.Description}",
                Type = NotificationType.MoneyReceived,
                ReferenceTransactionCode = @event.TransactionCode,
                Amount = @event.Amount,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            notifications.Add(receiverNotif);
        }

        if (notifications.Count > 0)
        {
            await _dbContext.Notifications.AddRangeAsync(notifications);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully created {Count} notification records for txn: {TxnCode}", notifications.Count, @event.TransactionCode);
        }
    }
}

public class WalletBalanceChangedConsumer : IIntegrationEventHandler<WalletBalanceChangedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<WalletBalanceChangedConsumer> _logger;

    public WalletBalanceChangedConsumer(
        NotificationDbContext dbContext,
        ILogger<WalletBalanceChangedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(WalletBalanceChangedEvent @event)
    {
        _logger.LogInformation("Processing WalletBalanceChangedEvent for user {UserId}, Type: {Type}", @event.UserId, @event.ChangeType);

        var isCredit = @event.ChangeType.Equals("Credit", StringComparison.OrdinalIgnoreCase);
        var title = isCredit ? "Nạp tiền vào ví MoMo thành công" : "Rút tiền từ ví MoMo thành công";
        var type = isCredit ? NotificationType.DepositSuccess : NotificationType.WithdrawSuccess;
        var prefix = isCredit ? "+" : "-";

        var notif = new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = title,
            Content = $"Ví MoMo: {prefix}{@event.Amount:N0} VND. Số dư mới: {@event.NewBalance:N0} VND. Mã giao dịch: {@event.TransactionCode}",
            Type = type,
            ReferenceTransactionCode = @event.TransactionCode,
            Amount = @event.Amount,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Notifications.AddAsync(notif);
        await _dbContext.SaveChangesAsync();
    }
}

public class UserRegisteredConsumer : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(NotificationDbContext dbContext, ILogger<UserRegisteredConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent @event)
    {
        _logger.LogInformation("Processing UserRegisteredEvent for welcome notification: {UserId}", @event.UserId);

        var notif = new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Chào mừng bạn đến với Ví điện tử MoMo!",
            Content = $"Xin chào {@event.FullName}, tài khoản MoMo của bạn (SĐT: {@event.PhoneNumber}) đã được kích hoạt thành công. Bắt đầu nạp tiền và trải nghiệm thanh toán tức thì!",
            Type = NotificationType.SystemAlert,
            Amount = 0m,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Notifications.AddAsync(notif);
        await _dbContext.SaveChangesAsync();
    }
}
