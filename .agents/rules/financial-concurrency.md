# Financial Concurrency & Safety Rules for MomoPayment

Financial applications require zero tolerance for double spending, race conditions, negative balances, and data drift.

## 1. The Distributed Locking Invariant

### Single-Resource Locking
When updating a single wallet (Deposit or Withdraw):
```csharp
var lockKey = $"wallet_lock:{userId}";
var lockValue = Guid.NewGuid().ToString("N");
bool acquired = await _lockService.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(50));
if (!acquired) throw new ConcurrencyConflictException("System is processing another request for your wallet");
try
{
    // execute balance mutation
}
finally
{
    await _lockService.ReleaseLockAsync(lockKey, lockValue);
}
```

### Multi-Resource Locking (Deadlock Prevention)
When transferring funds between two wallets (Alice -> Bob), a deadlock occurs if Alice transfers to Bob while Bob concurrently transfers to Alice, and each holds one lock waiting for the other.

To prevent deadlocks:
1. Resources MUST be sorted lexicographically:
   ```csharp
   var sortedKeys = resourceKeys.Distinct().OrderBy(k => k, StringComparer.Ordinal).ToList();
   ```
2. Locks must be acquired in this exact deterministic order.
3. If any acquisition fails, all previously acquired locks in the batch MUST be released immediately before returning null.
4. Use `await using` or `using` on the returned `IDisposable` / `IAsyncDisposable` handle to guarantee cleanup.

## 2. Idempotency Flow

Clients must provide `X-Idempotency-Key: <UUID>` on all financial mutation requests.

1. **Step 1: Check Result Cache**:
   `var cached = await _idempotencyService.GetResultAsync<T>(idempotencyKey);`
   If cached result exists, immediately return it.

2. **Step 2: Acquire In-Flight Lock**:
   `bool acquired = await _idempotencyService.TryAcquireKeyAsync(idempotencyKey, TimeSpan.FromMinutes(5));`
   If not acquired, throw `DuplicateTransactionException()`.

3. **Step 3: Execution**:
   Perform business logic and database persistence.

4. **Step 4: Success Result Caching**:
   `await _idempotencyService.SaveResultAsync(idempotencyKey, response, TimeSpan.FromHours(24));`

5. **Step 5: Rollback on Validation / Execution Error**:
   If execution aborts before state changes, release key:
   `await _idempotencyService.ReleaseKeyAsync(idempotencyKey);`

## 3. Balance Calculations & Monetary Representation

- Always use .NET `decimal` for monetary values. Never cast to `float` or perform calculations in `double`.
- In Protobuf messages, values must be safely converted to and from `decimal`.
- Verify `wallet.Balance >= amount` BEFORE performing subtraction.
- In `WalletLedger`, verify that:
  - For Credit: `NewBalance == PreviousBalance + Amount`
  - For Debit: `NewBalance == PreviousBalance - Amount`
