# MomoPayment - Microservices Payment Ecosystem

Dự án mô phỏng hệ sinh thái ứng dụng thanh toán ví điện tử **MoMo** được xây dựng trên nền tảng **C# / .NET 10** theo kiến trúc **Microservices chuẩn doanh nghiệp**. Hệ thống hỗ trợ xử lý bất đồng bộ cao độ, bảo toàn tính nhất quán dữ liệu tài chính, chống race conditions và double spending.

---

## 🌟 Công Nghệ & Tiêu Chuẩn Kỹ Thuật

- **Ngôn ngữ & Nền tảng:** C# (.NET 10 Web API, Minimal API & Controllers)
- **Kiến trúc:** Microservices Architecture (Event-Driven & RPC)
- **Cơ sở dữ liệu:** Microsoft SQL Server 2022 (Mô hình *Database per Service* độc lập: `Momo_IdentityDb`, `Momo_WalletDb`, `Momo_TransactionDb`, `Momo_NotificationDb`)
- **Bộ nhớ đệm & Khóa phân tán:** Redis 7 (Caching dữ liệu người dùng, Redis Distributed Lock đa tài nguyên chống Deadlock, Idempotency Key chống click đúp)
- **Message Broker:** RabbitMQ 3 Management (Xử lý sự kiện bất đồng bộ qua Exchange & Queues: `TransactionCompletedEvent`, `WalletBalanceChangedEvent`)
- **Giao tiếp nội bộ (Internal RPC):** gRPC với Protocol Buffers (`wallet.proto`) chạy qua HTTP/2 cho độ trễ cực thấp giữa Transaction Service và Wallet Service
- **API Gateway:** YARP (Yet Another Reverse Proxy) định tuyến toàn bộ Client traffic và tổng hợp tài liệu Swagger UI
- **Containerization:** Docker & Docker Compose đa dịch vụ, multi-stage build

---

## 📁 Cấu Trúc Thư Mục Dự Án

```
MomoPayment/
├── docs/
│   ├── DEPLOYMENT.md            # Hướng dẫn chi tiết triển khai Local, Docker, Troubleshooting & Production
│   ├── ARCHITECTURE.md          # Chi tiết thiết kế kiến trúc Microservices, gRPC, Redis, RabbitMQ
│   ├── FLOW.md                  # Sơ đồ tuần tự (Sequence Diagram) các luồng nạp, rút, chuyển tiền P2P
│   └── API_SPEC.md              # Đặc tả API chi tiết & các lệnh cURL test
├── docker/
│   └── sqlserver/
│       └── init-db.sql          # Script khởi tạo các CSDL độc lập cho từng microservice
├── src/
│   ├── BuildingBlocks/
│   │   ├── MomoPayment.Common/  # Model chuẩn ApiResponse, Exceptions, Security (JWT, Hash)
│   │   ├── MomoPayment.Caching/ # Redis Cache, Redis Distributed Lock, Redis Idempotency
│   │   ├── MomoPayment.EventBus/# RabbitMQ Connection, Publisher, Subscriber, IntegrationEvents
│   │   └── MomoPayment.Protos/  # Protobuf file wallet.proto & gRPC contract
│   ├── Services/
│   │   ├── Identity/
│   │   │   └── Identity.API/    # Đăng ký SĐT, Đăng nhập, cấp JWT, tra cứu người dùng
│   │   ├── Wallet/
│   │   │   └── Wallet.API/      # Quản lý số dư, Nạp/Rút, Sổ cái, gRPC Server (Port: 5002 / 50052)
│   │   ├── Transaction/
│   │   │   └── Transaction.API/ # Chuyển tiền P2P, Thanh toán đơn hàng, gọi gRPC, bắn RabbitMQ (Port: 5003)
│   │   └── Notification/
│   │       └── Notification.API/# RabbitMQ Event Consumer, hộp thư thông báo biến động số dư (Port: 5004)
│   ├── ApiGateways/
│   │   └── ApiGateway/          # YARP Gateway, tổng hợp Swagger (Port: 5000)
│   └── Web/
│       └── momo-web/            # React + Vite + TypeScript MoMo Web UI (Port: 3000)
├── docker-compose.yml           # Khởi chạy toàn bộ hạ tầng và các microservices
├── Directory.Build.props
├── Directory.Packages.props
├── MomoPayment.sln
└── README.md
```

---

## 🚀 Hướng Dẫn Khởi Chạy (Quick Start)

### Cách 1: Khởi chạy bằng Docker Compose (Khuyên dùng)

Chỉ với 1 lệnh duy nhất, Docker sẽ khởi động SQL Server, Redis, RabbitMQ, API Gateway, 4 Microservices và Web UI:

```bash
cd E:\Project\MomoPayment
docker compose up -d --build
```

Kiểm tra trạng thái các container:
```bash
docker compose ps
```

Các cổng truy cập dịch vụ:
- **MoMo Web UI (Dashboard người dùng):** [http://localhost:3000](http://localhost:3000)
- **API Gateway (Swagger UI tập trung):** [http://localhost:5000](http://localhost:5000)
- **RabbitMQ Management Dashboard:** [http://localhost:15672](http://localhost:15672) (User: `guest`, Password: `guest`)
- **SQL Server 2022:** `localhost:1433` (User: `sa`, Password: `MomoSecureP@ssw0rd!`)
- **Redis Server:** `localhost:6379`

---

### Cách 2: Khởi chạy cục bộ bằng .NET CLI

Nếu bạn đã có SQL Server, Redis và RabbitMQ đang chạy trên máy:

1. Chạy API Gateway:
   ```bash
   dotnet run --project src/ApiGateways/ApiGateway/ApiGateway.csproj
   ```
2. Chạy Identity Service:
   ```bash
   dotnet run --project src/Services/Identity/Identity.API/Identity.API.csproj
   ```
3. Chạy Wallet Service:
   ```bash
   dotnet run --project src/Services/Wallet/Wallet.API/Wallet.API.csproj
   ```
4. Chạy Transaction Service:
   ```bash
   dotnet run --project src/Services/Transaction/Transaction.API/Transaction.API.csproj
   ```
5. Chạy Notification Service:
   ```bash
   dotnet run --project src/Services/Notification/Notification.API/Notification.API.csproj
   ```

---

## 🧪 Kịch Bản Demo: Chuyển Tiền MoMo Sang MoMo

Hệ thống đã tạo sẵn tài khoản:
- **Người gửi (Alice):** SĐT `0901234567` (Số dư ban đầu: **5,000,000 VND**)
- **Người nhận (Bob):** SĐT `0907654321` (Số dư ban đầu: **2,000,000 VND**)
- Mật khẩu chung: `Password123@`

### Bước 1: Người gửi đăng nhập để lấy JWT Token
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"phoneNumber\":\"0901234567\",\"password\":\"Password123@\"}"
```
*(Copy giá trị `accessToken` nhận được).*

### Bước 2: Kiểm tra số dư hiện tại của Alice
```bash
curl -X GET http://localhost:5000/api/wallet/balance \
  -H "Authorization: Bearer <ACCESS_TOKEN>"
```

### Bước 3: Tra cứu thông tin người nhận trước khi chuyển
```bash
curl -X GET "http://localhost:5000/api/users/lookup?phone=0907654321" \
  -H "Authorization: Bearer <ACCESS_TOKEN>"
```
*(Hệ thống trả về tên: Trần Thị Bình, SĐT ẩn: `090****321`).*

### Bước 4: Thực hiện chuyển tiền P2P kèm Idempotency Key
```bash
curl -X POST http://localhost:5000/api/transactions/transfer \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "X-Idempotency-Key: f1234567-89ab-cdef-0123-456789abcdef" \
  -d "{\"receiverPhone\":\"0907654321\",\"amount\":500000,\"description\":\"Tra tien an toi hom qua\"}"
```
- Transaction Service gọi gRPC sang Wallet Service.
- Wallet Service lấy Redis Distributed Lock cho cả 2 ví, trừ 500k của Alice, cộng 500k cho Bob trong 1 Transaction CSDL.
- Bắn `TransactionCompletedEvent` sang RabbitMQ không đồng bộ.

### Bước 5: Đăng nhập tài khoản Bob để xem biến động số dư và thông báo
1. Đăng nhập Bob (`0907654321` / `Password123@`) lấy Token Bob.
2. Kiểm tra số dư Bob:
   ```bash
   curl -X GET http://localhost:5000/api/wallet/balance \
     -H "Authorization: Bearer <TOKEN_BOB>"
   ```
   *(Số dư mới: **2,500,000 VND**).*
3. Kiểm tra hộp thư thông báo RabbitMQ đã tạo:
   ```bash
   curl -X GET http://localhost:5000/api/notifications \
     -H "Authorization: Bearer <TOKEN_BOB>"
   ```
   *(Thông báo: `Bạn vừa nhận được +500,000 VND từ 0901234567. Lời nhắn: Tra tien an toi hom qua`).*

---

## 📑 Tài Liệu Tham Khảo Chi Tiết
- 🚀 **[Hướng Dẫn Triển Khai Toàn Diện (Deployment Guide)](docs/DEPLOYMENT.md)** *(Chi tiết Docker Compose, Hybrid Local Dev, Xử lý sự cố ports/database, Production Hardening)*
- 🏛️ **[Tài liệu Kiến Trúc Microservices](docs/ARCHITECTURE.md)**
- 🔄 **[Tài liệu Sơ Đồ Tuần Tự & Các Luồng Nghiệp Vụ](docs/FLOW.md)**
- 🔌 **[Đặc Tả Danh Sách API Chi Tiết](docs/API_SPEC.md)**
- 🧪 **Script Kiểm Thử Tự Động:** `.agents/skills/momo-payment-ops/scripts/test-api-flow.ps1`
