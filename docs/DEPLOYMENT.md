# Hướng Dẫn Triển Khai Hệ Thống MomoPayment Microservices (Deployment Guide)

Tài liệu này cung cấp hướng dẫn toàn diện từ A đến Z để thiết lập, cấu hình, triển khai và vận hành hệ sinh thái thanh toán ví điện tử **MomoPayment Microservices** trên cả môi trường phát triển cục bộ (**Local Development**) và môi trường đóng gói container (**Docker / Staging / Production**).

---

## 📑 Mục Lục
1. [Kiến Trúc & Topology Dịch Vụ](#1-kiến-trúc--topology-dịch-vụ)
2. [Yêu Cầu Hệ Thống & Tiền Đề (Prerequisites)](#2-yêu-cầu-hệ-thống--tiền-đề-prerequisites)
3. [Bảng Cổng (Port Mapping) & Mạng Lưới](#3-bảng-cổng-port-mapping--mạng-lưới)
4. [Cấu Hình & Danh Mục Biến Môi Trường](#4-cấu-hình--danh-mục-biến-môi-trường)
5. [Phương Thức 1: Triển Khai Toàn Diện Với Docker Compose (Khuyên Dùng)](#5-phương-thức-1-triển-khai-toàn-diện-với-docker-compose-khuyên-dùng)
6. [Phương Thức 2: Triển Khai Kết Hợp Cho Lập Trình Viên (Hybrid Local Dev)](#6-phương-thức-2-triển-khai-kết-hợp-cho-lập-trình-viên-hybrid-local-dev)
7. [Kiểm Thử & Xác Minh Triển Khai (Post-Deployment Verification)](#7-kiểm-thử--xác-minh-triển-khai-post-deployment-verification)
8. [Cẩm Nang Xử Lý Sự Cố (Troubleshooting & FAQs)](#8-cẩm-nang-xử-lý-sự-cố-troubleshooting--faqs)
9. [Khuyến Nghị Cho Môi Trường Sản Xuất (Production Hardening)](#9-khuyến-nghị-cho-môi-trường-sản-xuất-production-hardening)

---

## 1. Kiến Trúc & Topology Dịch Vụ

Hệ thống bao gồm 10 dịch vụ phối hợp trong mạng Docker nội bộ (`momo-network`):

```mermaid
graph TD
    Client["Browser / Client Apps"] -->|HTTP :3000| WebUI["momo-web (React + Vite + Nginx)"]
    Client -->|HTTP :5000| Gateway["ApiGateway (YARP Reverse Proxy)"]

    Gateway -->|/api/auth & /api/users| IdentityAPI["Identity.API (:5001)"]
    Gateway -->|/api/wallet| WalletAPI["Wallet.API (:5002)"]
    Gateway -->|/api/transactions| TxnAPI["Transaction.API (:5003)"]
    Gateway -->|/api/notifications| NotifAPI["Notification.API (:5004)"]

    TxnAPI -->|gRPC :50052 (HTTP/2 Cleartext)| WalletAPI

    IdentityAPI -->|Publish UserRegisteredEvent| RabbitMQ["RabbitMQ 3 (:5672/:15672)"]
    TxnAPI -->|Publish TransactionCompletedEvent| RabbitMQ
    WalletAPI -->|Publish WalletBalanceChangedEvent| RabbitMQ

    RabbitMQ -->|Consume UserRegisteredEvent| WalletAPI
    RabbitMQ -->|Consume All Events| NotifAPI

    WalletAPI <-->|Redlock & Idempotency| Redis["Redis 7 (:6379)"]
    TxnAPI <-->|Idempotency Cache| Redis
    IdentityAPI <-->|Cache User Profile| Redis

    IdentityAPI --> Momo_IdentityDb[("SQL Server: Momo_IdentityDb")]
    WalletAPI --> Momo_WalletDb[("SQL Server: Momo_WalletDb")]
    TxnAPI --> Momo_TransactionDb[("SQL Server: Momo_TransactionDb")]
    NotifAPI --> Momo_NotificationDb[("SQL Server: Momo_NotificationDb")]
```

---

## 2. Yêu Cầu Hệ Thống & Tiền Đề (Prerequisites)

### 2.1 Cấu Hình Phần Cứng Khuyến Nghị
- **CPU:** Tối thiểu 4 Cores (Khuyến nghị 6-8 Cores).
- **RAM:** Tối thiểu 8 GB RAM trống (SQL Server 2022 yêu cầu tối thiểu 2GB RAM; các microservices .NET 10 và Redis/RabbitMQ chiếm khoảng 2.5 - 3.5GB).
- **Dung lượng đĩa trống:** Tối thiểu 10 GB (cho Docker images, database volumes và dependencies).

### 2.2 Phần Mềm Cần Cài Đặt
| Phần mềm | Phiên bản yêu cầu | Mục đích |
| :--- | :--- | :--- |
| **Docker Engine & Docker Compose** | Docker Desktop 4.25+ (Compose v2) | Đóng gói và chạy toàn bộ dịch vụ |
| **.NET SDK** *(Tùy chọn cho Dev)* | .NET 10.0 SDK | Biên dịch và chạy source code backend cục bộ |
| **Node.js & npm** *(Tùy chọn cho Dev)* | Node.js >= 20.x, npm >= 10.x | Biên dịch và chạy giao diện `momo-web` |
| **PowerShell hoặc Bash** | PowerShell 7+ hoặc Bash | Thực thi các script kiểm thử tự động |

---

## 3. Bảng Cổng (Port Mapping) & Mạng Lưới

Khi chạy trên máy chủ hoặc localhost, hãy đảm bảo các cổng sau không bị chiếm dụng bởi phần mềm khác:

| Cổng Host | Dịch Vụ | Giao Thức / Mục Đích | Tài Khoản Mặc Định (Nếu có) |
| :---: | :--- | :--- | :--- |
| **`3000`** | **momo-web** | Web App Dashboard người dùng | Đăng nhập bằng tài khoản mẫu |
| **`5000`** | **ApiGateway** | YARP Reverse Proxy & Tổng hợp Swagger UI | Không yêu cầu auth |
| **`5001`** | **Identity.API** | REST API Danh tính, Người dùng | JWT Bearer |
| **`5002`** | **Wallet.API** | REST API Quản lý ví, Nạp / Rút | JWT Bearer |
| **`50052`**| **Wallet.API (gRPC)** | gRPC Internal RPC (HTTP/2 Cleartext) | Gọi nội bộ từ Transaction Service |
| **`5003`** | **Transaction.API** | REST API Chuyển tiền P2P, Thanh toán | JWT Bearer |
| **`5004`** | **Notification.API**| REST API Hộp thư thông báo | JWT Bearer |
| **`1433`** | **SQL Server 2022** | Microsoft SQL Server Database | User: `sa`<br>Pass: `MomoSecureP@ssw0rd!` |
| **`6379`** | **Redis 7** | Cache, Redlock, Idempotency keys | Không mật khẩu (Nội bộ) |
| **`5672`** | **RabbitMQ AMQP** | Giao thức trao đổi message | User: `guest`, Pass: `guest` |
| **`15672`**| **RabbitMQ Management**| Web UI quản lý Exchange & Queues | User: `guest`, Pass: `guest` |

---

## 4. Cấu Hình & Danh Mục Biến Môi Trường

Hệ thống có thể được cấu hình linh hoạt thông qua biến môi trường trong file `docker-compose.yml` hoặc file cấu hình `appsettings.json`:

### 4.1 Cấu Hình Cơ Sở Dữ Liệu (SQL Server)
```env
ACCEPT_EULA=Y
MSSQL_SA_PASSWORD=MomoSecureP@ssw0rd!
MSSQL_PID=Developer
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Momo_{Service}Db;User Id=sa;Password=MomoSecureP@ssw0rd!;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

### 4.2 Cấu Hình Redis & Khóa Phân Tán
```env
Redis__ConnectionString=redis:6379
Redis__InstanceName=Momo_{Service}_
```

### 4.3 Cấu Hình RabbitMQ Event Bus
```env
RabbitMq__HostName=rabbitmq
RabbitMq__Port=5672
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

### 4.4 Cấu Hình Bảo Mật JWT (Identity.API)
```json
"JwtOptions": {
  "SecretKey": "SuperSecretKeyForMomoPaymentMicroservicesEcosystem2026!@#",
  "Issuer": "MomoPayment.Identity",
  "Audience": "MomoPayment.Client",
  "ExpiryMinutes": 1440
}
```

---

## 5. Phương Thức 1: Triển Khai Toàn Diện Với Docker Compose (Khuyên Dùng)

Phương thức này dựng toàn bộ hệ thống gồm 10 containers chỉ bằng một câu lệnh.

### Bước 1: Clone mã nguồn hoặc chuyển đến thư mục dự án
```bash
cd E:\Project\MomoPayment
```

### Bước 2: Khởi chạy toàn bộ hệ thống
Khởi động container ở chế độ nền (detached mode) và build lại image mới nhất:
```bash
docker compose up -d --build
```
> **Mẹo:** Bạn cũng có thể dùng script PowerShell có sẵn:
> ```powershell
> .\.agents\skills\momo-payment-ops\scripts\start-docker.ps1
> ```

### Bước 3: Kiểm tra trạng thái khởi động của các Container
Chờ khoảng 10-15 giây để SQL Server và RabbitMQ hoàn tất quá trình khởi tạo cấu trúc dữ liệu, sau đó kiểm tra:
```bash
docker compose ps
```
Tất cả các container phải hiển thị trạng thái `Up` hoặc `healthy`:
```text
NAME                        STATUS              PORTS
momo-sqlserver              Up (healthy)        0.0.0.0:1433->1433/tcp
momo-redis                  Up (healthy)        0.0.0.0:6379->6379/tcp
momo-rabbitmq               Up (healthy)        0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
momo-identity-service       Up                  0.0.0.0:5001->5001/tcp
momo-wallet-service         Up                  0.0.0.0:5002->5002/tcp, 0.0.0.0:50052->50052/tcp
momo-transaction-service    Up                  0.0.0.0:5003->5003/tcp
momo-notification-service   Up                  0.0.0.0:5004->5004/tcp
momo-api-gateway            Up                  0.0.0.0:5000->5000/tcp
momo-web-ui                 Up                  0.0.0.0:3000->80/tcp
```

### Bước 4: Kiểm tra log của từng service nếu cần
```bash
# Xem log API Gateway
docker compose logs -f api-gateway

# Xem log Wallet Service (gRPC + Redlock)
docker compose logs -f wallet-service

# Xem log Transaction Service
docker compose logs -f transaction-service
```

### Bước 5: Truy cập các Dashboard giao diện
1. **Giao diện người dùng MoMo Web:** Mở trình duyệt tại [http://localhost:3000](http://localhost:3000)
2. **Swagger UI tổng hợp qua API Gateway:** [http://localhost:5000](http://localhost:5000)
3. **RabbitMQ Management Dashboard:** [http://localhost:15672](http://localhost:15672) (User: `guest`, Password: `guest`)

---

## 6. Phương Thức 2: Triển Khai Kết Hợp Cho Lập Trình Viên (Hybrid Local Dev)

Phương thức này dành cho việc phát triển và debug mã nguồn C# hoặc React: chạy các dịch vụ hạ tầng (SQL, Redis, RabbitMQ) bằng Docker, trong khi chạy các mã nguồn microservices bằng .NET CLI và Vite để hỗ trợ Hot Reload và Breakpoint Debugger.

### Bước 1: Khởi động 3 dịch vụ hạ tầng
```bash
docker compose up -d sqlserver redis rabbitmq
```
Đảm bảo 3 container đã sẵn sàng:
- SQL Server: `localhost:1433`
- Redis: `localhost:6379`
- RabbitMQ: `localhost:5672`

### Bước 2: Khởi động các Microservices Backend
Mở các tab terminal riêng biệt và chạy lần lượt:

```bash
# Terminal 1: API Gateway (Port 5000)
dotnet run --project src/ApiGateways/ApiGateway/ApiGateway.csproj

# Terminal 2: Identity Service (Port 5001)
dotnet run --project src/Services/Identity/Identity.API/Identity.API.csproj

# Terminal 3: Wallet Service (Port 5002 / gRPC 50052)
dotnet run --project src/Services/Wallet/Wallet.API/Wallet.API.csproj

# Terminal 4: Transaction Service (Port 5003)
dotnet run --project src/Services/Transaction/Transaction.API/Transaction.API.csproj

# Terminal 5: Notification Service (Port 5004)
dotnet run --project src/Services/Notification/Notification.API/Notification.API.csproj
```

### Bước 3: Khởi động Frontend Web App
Mở một tab terminal mới:
```bash
cd src/Web/momo-web
npm install
npm run dev
```
Giao diện Vite Dev Server sẽ chạy tại [http://localhost:3000](http://localhost:3000) với tính năng Hot Module Replacement (HMR).

---

## 7. Kiểm Thử & Xác Minh Triển Khai (Post-Deployment Verification)

### 7.1 Kiểm Tra Endpoint Health của API Gateway
Gửi request GET đến `/api/health`:
```bash
curl http://localhost:5000/api/health
```
Phản hồi mẫu thành công:
```json
{
  "status": "Healthy",
  "service": "ApiGateway",
  "timestamp": "2026-10-25T07:35:00.0000000Z"
}
```

### 7.2 Danh Sách Tài Khoản Thử Nghiệm Sẵn Có
Hệ thống đã tự động gieo dữ liệu (seed data) sẵn các tài khoản sau:

| Tên người dùng | Số điện thoại | Mật khẩu | Số dư khởi nguyên | Vai trò trong kịch bản |
| :--- | :---: | :---: | :---: | :--- |
| **Nguyễn Văn An (Alice)** | `0901234567` | `Password123@` | **5,000,000 VND** | Người gửi tiền, nạp tiền |
| **Trần Thị Bình (Bob)** | `0907654321` | `Password123@` | **2,000,000 VND** | Người nhận chuyển tiền |
| **Lê Hoàng Cường (Charlie)**| `0909999999` | `Password123@` | **10,000,000 VND**| Khách hàng VIP |

### 7.3 Chạy Kịch Bản Kiểm Thử Tự Động (E2E Integration Test)
Dự án đi kèm một script kiểm thử tự động kiểm tra trọn vẹn chu trình thanh toán:
- Đăng nhập người dùng (Identity)
- Tra cứu số dư (Wallet)
- Tra cứu thông tin người nhận (Identity lookup)
- Thực hiện giao dịch chuyển tiền P2P kèm khóa Idempotency Key (Transaction -> gRPC -> Wallet Redlock)
- Xác nhận biến động số dư người nhận và sự kiện thông báo RabbitMQ (Notification)

Chạy script trên PowerShell:
```powershell
.\.agents\skills\momo-payment-ops\scripts\test-api-flow.ps1
```
Kết quả mong đợi:
```text
==========================================
 Running MomoPayment E2E Integration Test  
 Target Gateway: http://localhost:5000
==========================================

[1/5] Logging in as Alice (0901234567)...
-> Success! Alice Logged In. Name: Nguyễn Văn An

[2/5] Checking Alice's initial balance...
-> Alice Balance: 5,000,000 VND

[3/5] Looking up Bob's info by phone (0907654321)...
-> Recipient: Trần Thị Bình (090****321)

[4/5] Transferring 500,000 VND to Bob with Idempotency Key: ...
-> Transfer Succeeded! Txn Code: TXN-...

[5/5] Logging in as Bob to verify balance and RabbitMQ notification...
-> Bob New Balance: 2,500,000 VND
-> Bob Notifications Count: 1
-> Latest Notification: Nhận tiền thành công - Bạn vừa nhận được +500,000 VND từ 0901234567

==========================================
 ALL TESTS PASSED! MICROSERVICES ARE HEALTHY 
==========================================
```

---

## 8. Cẩm Nang Xử Lý Sự Cố (Troubleshooting & FAQs)

### 🔴 Sự Cố 1: Lỗi Xung Đột Cổng (`port is already allocated`)
- **Triệu chứng:** Khi chạy `docker compose up`, Docker báo lỗi `Bind for 0.0.0.0:1433 failed: port is already allocated` hoặc tương tự với cổng 5000, 6379, 15672.
- **Nguyên nhân:** Có dịch vụ SQL Server cục bộ, Redis hoặc IIS đang chiếm dụng cổng trên máy chủ.
- **Cách khắc phục trên Windows:**
  ```powershell
  # Tìm tiến trình đang chiếm cổng (ví dụ cổng 1433)
  netstat -ano | findstr :1433
  # Dừng tiến trình bằng PID tìm được
  taskkill /PID <PID_TIM_THAY> /F
  ```
  Hoặc đổi cổng ánh xạ bên ngoài trong `docker-compose.yml` (ví dụ `"14333:1433"`).

---

### 🔴 Sự Cố 2: Microservice Bị Thoát Khi Cold-Start SQL Server
- **Triệu chứng:** Container backend (như `momo-wallet-service`) bị dừng với thông báo lỗi không thể kết nối database khi vừa bật Docker.
- **Nguyên nhân:** Container SQL Server 2022 cần từ 10 - 20 giây để khởi tạo database engine và chạy script `init-db.sql`.
- **Cơ chế tự phục hồi sẵn có:** Mã nguồn của từng microservice đã được trang bị vòng lặp retry tự động (`maxRetries = 10`) với khoảng nghỉ `Thread.Sleep(3000)`. Đồng thời `docker-compose.yml` có thiết lập `restart: unless-stopped` và Docker healthcheck cho SQL Server.
- **Cách khắc phục:** Chờ khoảng 15 giây rồi gõ `docker compose ps` để kiểm tra các container đã tự khởi động lại thành công.

---

### 🔴 Sự Cố 3: Lỗi Kết Nối gRPC (`HttpRequestException / Connection Refused`)
- **Triệu chứng:** Transaction Service trả về HTTP 500 khi gọi chuyển tiền, log báo không kết nối được `wallet-service:50052`.
- **Nguyên nhân:** Giao thức gRPC chạy trên HTTP/2 Cleartext (h2c) yêu cầu cấu hình endpoint Kestrel riêng biệt.
- **Kiểm tra cấu hình:** Đảm bảo `wallet-service` có biến môi trường:
  ```env
  Kestrel__Endpoints__gRPC__Url=http://0.0.0.0:50052
  Kestrel__Endpoints__gRPC__Protocols=Http2
  ```
  Và `transaction-service` có cấu hình:
  ```env
  GrpcClients__WalletServiceUrl=http://wallet-service:50052
  ```

---

### 🔴 Sự Cố 4: Xóa Toàn Bộ Dữ Liệu & Khởi Động Lại Từ Đầu (Hard Reset)
Nếu muốn dọn dẹp sạch toàn bộ cơ sở dữ liệu, các key Redis và hàng đợi RabbitMQ để kiểm thử mới từ đầu:
```bash
# Dừng và xóa toàn bộ container cùng với volume dữ liệu
docker compose down -v --remove-orphans

# Khởi động lại
docker compose up -d --build
```
> **Mẹo:** Bạn có thể chạy script tiện ích:
> ```powershell
> .\.agents\skills\momo-payment-ops\scripts\clean-all.ps1
> ```

---

## 9. Khuyến Nghị Cho Môi Trường Sản Xuất (Production Hardening)

Khi chuẩn bị đưa hệ thống lên môi trường điện toán đám mây (AWS, Azure, GCP, Kubernetes):

1. **Bảo Mật Khóa Bí Mật (Secret Management):**
   - Tuyệt đối không commit các mật khẩu mặc định (`MomoSecureP@ssw0rd!`, `SecretKey`) lên Git repository.
   - Sử dụng **Azure Key Vault**, **AWS Secrets Manager** hoặc **HashiCorp Vault** để nạp cấu hình thời gian chạy.

2. **Cấu Hình TLS/SSL (HTTPS):**
   - Đặt một NGINX Reverse Proxy, Cloudflare hoặc Ingress Controller phía trước API Gateway (cổng 5000) để xử lý chứng chỉ SSL (Let's Encrypt / Wildcard Certificate).

3. **Giới Hạn Tài Nguyên Container (Resource Quotas):**
   - Thiết lập giới hạn CPU và Memory trong `docker-compose.yml` hoặc Kubernetes Deployment:
     ```yaml
     deploy:
       resources:
         limits:
           cpus: '1.0'
           memory: 1024M
         reservations:
           cpus: '0.25'
           memory: 256M
     ```

4. **Sao Lưu Dữ Liệu & Cụm Khả Dụng Cao (High Availability):**
   - Thay thế SQL Server đơn lẻ bằng **SQL Server Always On Availability Groups** hoặc **Azure SQL Database**.
   - Thiết lập **Redis Sentinel** hoặc **Redis Cluster** cho Redlock đa node nhằm đảm bảo tính sẵn sàng cao khi một node Redis bị ngắt kết nối.
   - Cụm RabbitMQ Quorum Queues cho các sự kiện tài chính quan trọng.
