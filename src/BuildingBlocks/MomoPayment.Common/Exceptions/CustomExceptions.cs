namespace MomoPayment.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    public AppException(string message, int statusCode = 400, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message, string? errorCode = "NOT_FOUND")
        : base(message, 404, errorCode) { }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message, string? errorCode = "BAD_REQUEST")
        : base(message, 400, errorCode) { }
}

public class InsufficientBalanceException : AppException
{
    public InsufficientBalanceException(string message = "Số dư trong ví không đủ để thực hiện giao dịch", string? errorCode = "INSUFFICIENT_BALANCE")
        : base(message, 400, errorCode) { }
}

public class DuplicateTransactionException : AppException
{
    public DuplicateTransactionException(string message = "Giao dịch đang được xử lý hoặc đã hoàn tất (trùng lặp idempotency key)", string? errorCode = "DUPLICATE_TRANSACTION")
        : base(message, 409, errorCode) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Yêu cầu xác thực không hợp lệ", string? errorCode = "UNAUTHORIZED")
        : base(message, 401, errorCode) { }
}

public class ConcurrencyConflictException : AppException
{
    public ConcurrencyConflictException(string message = "Tài nguyên đang bị khóa bởi tiến trình khác, vui lòng thử lại sau giây lát", string? errorCode = "CONCURRENCY_CONFLICT")
        : base(message, 409, errorCode) { }
}
