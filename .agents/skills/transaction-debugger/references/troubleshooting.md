# MomoPayment Troubleshooting Matrix

| Symptom | Probable Cause | Diagnostic Command / Check | Resolution |
| :--- | :--- | :--- | :--- |
| **HTTP 500 / SqlException: Cannot open database** on fresh start | SQL Server container still starting when API starts | `docker compose logs wallet-service` | The built-in 10-attempt retry loop will automatically reconnect. If running locally, ensure SQL Server service is running. |
| **HTTP 409: DUPLICATE_TRANSACTION on retry after validation failure** | Idempotency key was not released upon failure | Check Redis `idempotency:<KEY>` | Use `IIdempotencyService.ReleaseKeyAsync(key)` in error handling paths so user can retry immediately. |
| **HTTP 409: CONCURRENCY_CONFLICT** | Wallet distributed lock held or high contention | Check Redis `TTL lock:wallet_lock:<userId>` | Lock auto-expires in 10s. For transfers, ensure alphabetical ordering of resource keys. |
| **HTTP 404: RECIPIENT_WALLET_NOT_FOUND** | Receiver has registered in Identity DB but wallet was not initialized | Query `Momo_WalletDb.Wallets` for phone | Use `GetOrCreateWalletAsync` or initialize wallet upon registration. |
| **Swagger UI CORS error on port 5000** | Browser blocked cross-origin request to ports 5001-5004 | Check DevTools Console | Ensure `app.UseCors("AllowAll")` is enabled in each microservice. |
| **RabbitMQ message lost or unacknowledged** | Consumer crashed or threw during processing | Check RabbitMQ Management queue `notification_service_queue` | Check `Notification.API` logs for serialization or database errors. |
