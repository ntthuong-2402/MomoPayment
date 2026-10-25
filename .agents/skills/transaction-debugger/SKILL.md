---
name: transaction-debugger
description: >-
  Use this skill when investigating, debugging, or troubleshooting payment transaction issues,
  concurrency lock timeouts, double-spending anomalies, or RabbitMQ event propagation failures.
---

# Transaction Debugger Skill

This skill guides the agent through isolating and debugging issues in the MomoPayment distributed payment pipeline.

---

## 1. Trace a Transaction by Code

When a transaction fails or is stuck in `Pending`:

1. **Query Transaction API**:
   ```http
   GET http://localhost:5000/api/transactions/code/{transactionCode}
   Authorization: Bearer <USER_TOKEN>
   ```
2. **Inspect the Transaction Status & Failure Reason**:
   - `Pending`: gRPC call or database commit did not finalize.
   - `Failed`: Check the `failureReason` field (e.g. `INSUFFICIENT_BALANCE`, `RECIPIENT_WALLET_LOCKED`).
   - `Success`: Funds were transferred in `Momo_WalletDb`.

---

## 2. Check Wallet Ledger Audit Trail

To verify ledger math and determine whether funds were actually debited or credited:

```http
GET http://localhost:5000/api/wallet/ledger?page=1&pageSize=20
Authorization: Bearer <USER_TOKEN>
```

Verify that:
- Each credit has an exact matching transaction code.
- `NewBalance == PreviousBalance ± Amount`.

---

## 3. Diagnose Distributed Lock & Redis Issues

If a user gets `CONCURRENCY_CONFLICT` ("Hệ thống đang xử lý giao dịch khác"):

1. Check Redis keys for lingering locks:
   ```bash
   redis-cli -h localhost -p 6379 KEYS "lock:wallet_lock:*"
   ```
2. Check TTL of active lock:
   ```bash
   redis-cli -h localhost -p 6379 TTL "lock:wallet_lock:<USER_GUID>"
   ```
3. Check Idempotency key state:
   ```bash
   redis-cli -h localhost -p 6379 GET "idempotency:<KEY>"
   redis-cli -h localhost -p 6379 GET "idempotency:<KEY>:result"
   ```

---

## 4. Diagnose RabbitMQ Event Delivery

If notifications are not appearing in the user's inbox:

1. Open RabbitMQ Management UI at `http://localhost:15672` (Credentials: `guest` / `guest`).
2. Navigate to **Exchanges** -> click `momo.events.exchange`.
3. Check bindings for:
   - `TransactionCompletedEvent` -> `notification_service_queue`
   - `WalletBalanceChangedEvent` -> `notification_service_queue`
4. Inspect the queue queue depth: if unacknowledged messages exist, inspect `Notification.API` logs for unhandled consumer exceptions.

For more detailed failure patterns and mitigation steps, refer to:
[troubleshooting.md](./references/troubleshooting.md)
