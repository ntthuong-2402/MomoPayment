---
name: momo-payment-ops
description: >-
  Use this skill when running, building, testing, or diagnosing the MomoPayment microservices ecosystem,
  including Docker Compose deployment, local development service runs, database initialization, and end-to-end API testing.
---

# MomoPayment Operations & Runbook Skill

This skill provides step-by-step procedures, commands, and automation scripts for managing the MomoPayment microservices stack.

---

## 1. Quick Verification & Build

To verify that all 9 projects in the solution compile cleanly without warnings:

```powershell
dotnet build MomoPayment.slnx
```

Or using the classic solution file:
```powershell
dotnet build MomoPayment.sln
```

---

## 2. Docker Compose Deployment (Recommended)

To start SQL Server 2022, Redis, RabbitMQ, and all 5 microservices:

```powershell
# Run using helper script
.\.agents\skills\momo-payment-ops\scripts\start-docker.ps1
```

Or manually:
```powershell
docker compose up -d --build
```

### Checking Service Health & Logs
```powershell
# View running containers
docker compose ps

# View logs for a specific service
docker compose logs -f api-gateway
docker compose logs -f wallet-service
docker compose logs -f transaction-service
```

### Port Mappings
- **API Gateway (Swagger & reverse proxy):** `http://localhost:5000`
- **RabbitMQ Management Dashboard:** `http://localhost:15672` (`guest` / `guest`)
- **SQL Server 2022:** `localhost:1433` (`sa` / `MomoSecureP@ssw0rd!`)
- **Redis Server:** `localhost:6379`

---

## 3. Automated End-to-End Test Flow

To run an automated end-to-end test verifying:
1. Alice login (`0901234567` / `Password123@`)
2. Initial balance inspection
3. Recipient phone lookup for Bob (`0907654321`)
4. Idempotent transfer of 500,000 VND
5. Bob login and balance verification (+500,000 VND)
6. RabbitMQ notification message delivery to Bob's notification box

Execute the test script:
```powershell
.\.agents\skills\momo-payment-ops\scripts\test-api-flow.ps1
```

---

## 4. Local Development Without Docker

If running databases and brokers locally:

```powershell
# Start services in separate terminal windows or tabs:
dotnet run --project src/ApiGateways/ApiGateway/ApiGateway.csproj
dotnet run --project src/Services/Identity/Identity.API/Identity.API.csproj
dotnet run --project src/Services/Wallet/Wallet.API/Wallet.API.csproj
dotnet run --project src/Services/Transaction/Transaction.API/Transaction.API.csproj
dotnet run --project src/Services/Notification/Notification.API/Notification.API.csproj
```

---

## 5. Teardown and Cleanup

To stop and clean all containers and volumes:
```powershell
docker compose down -v
```
