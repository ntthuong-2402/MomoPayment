namespace MomoPayment.Common.Enums;

public enum TransactionType
{
    Deposit = 1,    // Nạp tiền từ ngân hàng liên kết vào ví
    Withdraw = 2,   // Rút tiền từ ví về ngân hàng
    Transfer = 3,   // Chuyển tiền P2P (MoMo sang MoMo)
    Payment = 4     // Thanh toán hóa đơn/dịch vụ
}

public enum TransactionStatus
{
    Pending = 1,    // Đang xử lý
    Success = 2,    // Thành công
    Failed = 3,     // Thất bại
    Cancelled = 4   // Đã hủy
}

public enum WalletStatus
{
    Active = 1,
    Locked = 2,
    Suspended = 3
}

public enum NotificationType
{
    MoneyReceived = 1,
    MoneySent = 2,
    DepositSuccess = 3,
    WithdrawSuccess = 4,
    PaymentSuccess = 5,
    SystemAlert = 6
}
