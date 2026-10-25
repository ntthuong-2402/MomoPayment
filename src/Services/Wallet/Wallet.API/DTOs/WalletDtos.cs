using System.ComponentModel.DataAnnotations;
using MomoPayment.Common.Enums;

namespace Wallet.API.DTOs;

public class WalletResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "VND";
    public WalletStatus Status { get; set; }
}

public class DepositRequest
{
    [Required]
    [Range(10000, 50000000, ErrorMessage = "Số tiền nạp tối thiểu là 10,000 VND và tối đa 50,000,000 VND")]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentSource { get; set; } = "Vietcombank (*1234)";

    public string? Description { get; set; }
}

public class WithdrawRequest
{
    [Required]
    [Range(10000, 50000000, ErrorMessage = "Số tiền rút tối thiểu là 10,000 VND và tối đa 50,000,000 VND")]
    public decimal Amount { get; set; }

    [Required]
    public string BankCode { get; set; } = "VCB";

    [Required]
    public string BankAccountNumber { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class LedgerResponse
{
    public Guid Id { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
