# MomoPayment Agent Guidelines & Operational Instructions

Welcome, Agent! You are operating in the **MomoPayment Microservices Payment Ecosystem** repository.
This system simulates the core backend architecture of a high-throughput digital wallet (MoMo) built with **C# / .NET 10**, SQL Server 2022, Redis 7, RabbitMQ, gRPC, and YARP API Gateway.

---

## 1. System Architecture & Service Topology

| Service | Type / Framework | Ports | Database / Persistence | Responsibilities |
| :--- | :--- | :--- | :--- | :--- |
| **ApiGateway** | YARP Reverse Proxy | `5000` | None | Client entrypoint, route proxying, CORS policy, Swagger UI aggregation |
| **Identity.API** | ASP.NET Core REST API | `5001` | `Momo_IdentityDb` | Phone-based registration, authentication, JWT issuing, user profile lookup |
| **Wallet.API** | REST API & gRPC Server | `5002` (REST), `50052` (gRPC) | `Momo_WalletDb` + Redis Cache | Wallet balance, Deposit/Withdraw, Ledger audit trail, internal gRPC RPC |
| **Transaction.API** | REST API & gRPC Client | `5003` | `Momo_TransactionDb` + Redis | P2P Transfer, payment orders, Idempotency checking, calls Wallet via gRPC |
| **Notification.API** | Background Consumer & REST | `5004` | `Momo_NotificationDb` | RabbitMQ event consumer, balance change & transaction notification box |

Infrastructure services:
- **SQL Server 2022**: Port `1433` (Databases: `Momo_IdentityDb`, `Momo_WalletDb`, `Momo_TransactionDb`, `Momo_NotificationDb`)
- **Redis 7**: Port `6379` (Distributed locks, idempotency keys, balance & profile cache)
- **RabbitMQ 3 Management**: Port `5672` (AMQP), Port `15672` (Web UI, guest/guest)

---

## 2. Mandatory Rules for Financial & Concurrency Integrity

When modifying or adding code to this repository, you MUST strictly adhere to these financial computing invariants:

### 2.1 Double-Spending Prevention & Distributed Locks
- Never mutate a user's wallet balance without acquiring a Redis Distributed Lock (`wallet_lock:{userId}`).
- For multi-wallet operations (such as P2P Transfer), **always acquire distributed locks in deterministic sorted order** (e.g., alphabetical by key) to mathematically eliminate circular wait deadlocks:
  ```csharp
  var sortedKeys = resourceKeys.Distinct().OrderBy(k => k, StringComparer.Ordinal);
  ```
- Always release distributed locks in a `finally` block or via `IAsyncDisposable`.

### 2.2 Idempotency Protocol
- Financial state-altering endpoints (Transfer, Payment, Deposit, Withdraw) MUST support an `X-Idempotency-Key` HTTP header.
- Flow:
  1. Check Redis for cached result (`idempotency:{key}:result`). If present, return immediately.
  2. Attempt `TryAcquireKeyAsync(key)` (sets state `PROCESSING` with TTL).
  3. If acquisition fails, throw `DuplicateTransactionException` (HTTP 409).
  4. If execution succeeds, save response via `SaveResultAsync(key, result, 24h)`.
  5. **CRITICAL**: If execution throws a validation or pre-transfer failure, you MUST call `ReleaseKeyAsync(key)` so the client can fix their input and retry without waiting 5 minutes.

### 2.3 Strict Database Ledger Immutability
- Every debit or credit to a wallet MUST produce a corresponding `WalletLedger` row in the same database transaction.
- Ledger rows must record: `PreviousBalance`, `NewBalance`, `Amount`, `Type` (`Credit`/`Debit`), and `TransactionCode`.
- Never execute `UPDATE` or `DELETE` queries on `WalletLedger`. It is an append-only audit trail.

### 2.4 gRPC & Inter-Service Communication
- Transaction Service calls Wallet Service via gRPC over HTTP/2 cleartext port `50052`.
- gRPC services must be lightweight and idempotent wherever possible.
- Avoid floating-point arithmetic pitfalls: always use `decimal` inside C# domain logic and database columns (`decimal(18, 2)`).

### 2.5 Event Publishing & Asynchronous Processing
- Use RabbitMQ for eventual consistency side-effects (e.g., push notifications, analytics, transaction history indexing).
- Always `await` the `_eventBus.PublishAsync(...)` call within the request handler before returning HTTP 200, ensuring the message has reached the durable direct exchange (`momo.events.exchange`).

---

## 3. Coding & Framework Standards

- **Language & Runtime:** C# 13 on .NET 10.
- **Null Safety:** Nullable reference types are enabled (`<Nullable>enable</Nullable>`). Avoid null-forgiving operators (`!`) unless guaranteed by preceding guards.
- **Async Pattern:** Always propagate `CancellationToken` from controller actions down to EF Core, Redis, and gRPC client calls.
- **Error Handling:** All controllers return standard `ApiResponse<T>`. Never return raw strings or uncaught exceptions; let `GlobalExceptionMiddleware` catch `AppException` and map to appropriate HTTP status codes.
- **Database Startup:** Use retry loops for `db.Database.EnsureCreated()` to handle cold starts when Docker containers spin up simultaneously.

---

## 4. Verification Workflow

Before considering any task complete:
1. Run `dotnet build MomoPayment.slnx` and confirm `0 Warning(s), 0 Error(s)`.
2. Ensure both `MomoPayment.slnx` and `MomoPayment.sln` are maintained in sync.
3. Test with the automated test script in `.agents/skills/momo-payment-ops/scripts/test-api-flow.ps1`.
