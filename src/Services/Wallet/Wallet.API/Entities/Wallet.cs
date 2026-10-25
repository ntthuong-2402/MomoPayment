using MomoPayment.Common.Enums;

namespace Wallet.API.Entities;

public class WalletAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0m;
    public string Currency { get; set; } = "VND";
    public WalletStatus Status { get; set; } = WalletStatus.Active;
    public byte[]? RowVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<WalletLedger> Ledgers { get; set; } = new List<WalletLedger>();
}

public class WalletLedger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletId { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // "Credit" (cộng tiền) or "Debit" (trừ tiền)
    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public WalletAccount Wallet { get; set; } = null!;
}
