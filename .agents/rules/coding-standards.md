# C# & .NET 10 Coding Standards for MomoPayment

These rules govern all code modifications, refactorings, and additions across the MomoPayment solution.

## 1. Project Organization & Clean Architecture

- **`BuildingBlocks/`**: Shared libraries with zero dependency on specific domain microservices.
  - `MomoPayment.Common`: Standard models, exceptions, security helpers (JWT, PBKDF2), middleware.
  - `MomoPayment.Caching`: Redis distributed lock, idempotency, caching implementations.
  - `MomoPayment.EventBus`: RabbitMQ connection management, event publisher/subscriber contracts.
  - `MomoPayment.Protos`: Protobuf contracts and generated gRPC clients/servers.
- **`Services/`**: Independent, self-contained microservices adhering to Database-per-Service.
  - Must never directly query another service's database.
  - Internal synchronous queries/commands go through gRPC (`wallet.proto`).
  - Asynchronous cross-service notifications go through RabbitMQ (`momo.events.exchange`).

## 2. API Design & Response Formatting

All controller endpoints must wrap output in `ApiResponse<T>` or `ApiResponse`:
```csharp
[HttpGet("profile")]
public async Task<ActionResult<ApiResponse<UserResponse>>> GetProfile(CancellationToken cancellationToken)
{
    var userId = JwtHelper.GetUserId(User);
    if (userId == null) return Unauthorized(ApiResponse.Fail("Unauthorized", "UNAUTHORIZED"));
    
    var profile = await _identityService.GetUserProfileAsync(userId.Value, cancellationToken);
    return Ok(ApiResponse<UserResponse>.Ok(profile, "Profile retrieved successfully"));
}
```

## 3. Exception Handling

- Do not return manual 500 error responses in controllers.
- Throw custom domain exceptions inheriting from `AppException`:
  - `NotFoundException`: maps to HTTP 404
  - `BadRequestException`: maps to HTTP 400
  - `InsufficientBalanceException`: maps to HTTP 400 with `INSUFFICIENT_BALANCE`
  - `DuplicateTransactionException`: maps to HTTP 409 with `DUPLICATE_TRANSACTION`
  - `ConcurrencyConflictException`: maps to HTTP 409 with `CONCURRENCY_CONFLICT`
- `GlobalExceptionMiddleware` intercepts these exceptions and serializes a clean, standardized JSON response.

## 4. Entity Framework Core Guidelines

- Use `AsNoTracking()` for read-only queries to prevent unnecessary change-tracking overhead.
- Always configure column precision for money: `.HasPrecision(18, 2)`.
- Use `RowVersion` timestamp tokens for optimistic concurrency checking.
- Wrap multi-table updates (e.g. Wallet account + Ledger row) in `await _dbContext.Database.BeginTransactionAsync(cancellationToken)`.
- On application startup, utilize the 10-attempt retry loop for `Database.EnsureCreated()` to accommodate container startup latencies.

## 5. Security & Tokens

- Always retrieve the authenticated user identity via `JwtHelper.GetUserId(User)` and `JwtHelper.GetPhoneNumber(User)`.
- Passwords must be hashed using PBKDF2 with SHA-256 and cryptographic salts (`PasswordHasher.cs`). Never store plaintext or MD5/SHA1 hashes.
- Never log plain passwords, JWT secret keys, or sensitive customer data.
