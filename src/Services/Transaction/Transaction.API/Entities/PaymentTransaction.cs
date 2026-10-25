using MomoPayment.Common.Enums;

namespace Transaction.API.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TransactionCode { get; set; } = string.Empty;
    public Guid? SenderUserId { get; set; }
    public string? SenderPhone { get; set; }
    public Guid? ReceiverUserId { get; set; }
    public string? ReceiverPhone { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; } = 0m;
    public decimal NetAmount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? FailureReason { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
