using System.ComponentModel.DataAnnotations;
using MomoPayment.Common.Enums;

namespace Transaction.API.DTOs;

public class TransferRequest
{
    [Required(ErrorMessage = "Số điện thoại người nhận là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required]
    [Range(1000, 50000000, ErrorMessage = "Số tiền chuyển tối thiểu là 1,000 VND và tối đa 50,000,000 VND/giao dịch")]
    public decimal Amount { get; set; }

    [MaxLength(200, ErrorMessage = "Lời nhắn tối đa 200 ký tự")]
    public string Description { get; set; } = "Chuyển tiền MoMo";

    public string? IdempotencyKey { get; set; }
}

public class PaymentRequest
{
    [Required(ErrorMessage = "Mã đơn hàng là bắt buộc")]
    public string OrderId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã đối tác / Merchant là bắt buộc")]
    public string MerchantId { get; set; } = string.Empty;

    [Required]
    [Range(1000, 100000000, ErrorMessage = "Số tiền thanh toán tối thiểu 1,000 VND")]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string Description { get; set; } = "Thanh toán dịch vụ";

    public string? IdempotencyKey { get; set; }
}

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public Guid? SenderUserId { get; set; }
    public string? SenderPhone { get; set; }
    public Guid? ReceiverUserId { get; set; }
    public string? ReceiverPhone { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal NetAmount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TransactionFilterRequest
{
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
