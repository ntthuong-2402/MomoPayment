using System.Text.Json.Serialization;

namespace MomoPayment.EventBus.Events;

public abstract record IntegrationEvent
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("creationDate")]
    public DateTime CreationDate { get; init; } = DateTime.UtcNow;
}

public record TransactionCompletedEvent : IntegrationEvent
{
    public Guid TransactionId { get; init; }
    public string TransactionCode { get; init; } = string.Empty;
    public Guid? SenderUserId { get; init; }
    public string? SenderPhone { get; init; }
    public Guid? ReceiverUserId { get; init; }
    public string? ReceiverPhone { get; init; }
    public decimal Amount { get; init; }
    public decimal Fee { get; init; }
    public string Type { get; init; } = string.Empty;       // Transfer, Payment, Deposit, Withdraw
    public string Status { get; init; } = string.Empty;     // Success, Failed
    public string? FailureReason { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}

public record UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
}

public record WalletBalanceChangedEvent : IntegrationEvent
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public decimal OldBalance { get; init; }
    public decimal NewBalance { get; init; }
    public decimal Amount { get; init; }
    public string ChangeType { get; init; } = string.Empty; // Credit, Debit
    public string TransactionCode { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
