# Chi Tiết Các Luồng Nghiệp Vụ Ví MoMo (Business Flows)

Tài liệu này mô tả chi tiết các luồng nghiệp vụ (Workflow) của ứng dụng thanh toán MoMo bằng sơ đồ tuần tự (Sequence Diagram) và các bước xử lý từng giai đoạn.

---

## 1. Luồng Đăng Ký Tài Khoản & Khởi Tạo Ví (Registration Flow)

Khi một khách hàng mới đăng ký tài khoản MoMo, hệ thống thực hiện mã hóa mật khẩu, tạo bản ghi người dùng và tự động kích hoạt ví với số dư ban đầu là 0 VND.

```mermaid
sequenceDiagram
    autonumber
    actor User as Khách hàng
    participant GW as API Gateway
    participant Identity as Identity Service
    participant Redis as Redis Cache
    participant DB_Id as Momo_IdentityDb

    User->>GW: POST /api/auth/register (SĐT, Mật khẩu, Họ tên, Email)
    GW->>Identity: Chuyển tiếp request
    Identity->>DB_Id: Kiểm tra SĐT đã tồn tại chưa?
    alt SĐT đã tồn tại
        DB_Id-->>Identity: Đã tồn tại
        Identity-->>GW: 400 Bad Request (PHONE_ALREADY_EXISTS)
        GW-->>User: Báo lỗi "Số điện thoại đã được đăng ký"
    else SĐT hợp lệ
        Identity->>Identity: Băm mật khẩu (PBKDF2 + Salt 16 bytes)
        Identity->>DB_Id: Insert User mới (Status: Active)
        Identity->>Identity: Tạo JWT Access Token
        Identity->>Redis: Cache thông tin người dùng
        Identity-->>GW: 200 OK + JWT Token + Thông tin tài khoản
        GW-->>User: Đăng ký thành công, tự động đăng nhập
    end
```

---

## 2. Luồng Nạp Tiền Vào Ví MoMo (Deposit / Top-up Flow)

Khách hàng nạp tiền từ tài khoản/thẻ ngân hàng liên kết vào ví MoMo. Luồng này sử dụng **Redis Distributed Lock** để tránh xung đột cập nhật số dư và ghi nhận sổ cái biến động (Ledger).

```mermaid
sequenceDiagram
    autonumber
    actor User as Khách hàng
    participant GW as API Gateway
    participant Wallet as Wallet Service
    participant Redis as Redis (Distributed Lock)
    participant DB_Wallet as Momo_WalletDb
    participant Rabbit as RabbitMQ (momo.events)
    participant Notif as Notification Service

    User->>GW: POST /api/wallet/deposit (Bearer Token, Amount, PaymentSource)
    GW->>Wallet: Chuyển tiếp request
    Wallet->>Wallet: Xác thực JWT -> Trích xuất UserId
    Wallet->>Redis: Xin Distributed Lock: lock:wallet_lock:{UserId} (TTL: 10s)
    alt Không lấy được Lock (Đang có giao dịch khác)
        Redis-->>Wallet: Lock Failed
        Wallet-->>User: 409 Conflict: "Hệ thống đang xử lý giao dịch khác..."
    else Lấy Lock thành công
        Redis-->>Wallet: Lock Acquired
        Wallet->>DB_Wallet: Lấy thông tin Ví hiện tại (Số dư cũ)
        Wallet->>Wallet: Cộng số dư: Balance = Balance + Amount
        Wallet->>DB_Wallet: Cập nhật Balance mới vào bảng Wallets
        Wallet->>DB_Wallet: Insert bản ghi Sổ cái (WalletLedger: Type='Credit')
        Wallet->>Redis: Xóa Cache số dư cũ (wallet_balance:{UserId})
        Wallet->>Redis: Giải phóng Lock (LockRelease)
        
        Note over Wallet,Rabbit: Bắn sự kiện bất đồng bộ
        Wallet-)Rabbit: Publish WalletBalanceChangedEvent (Type='Credit', Amount, NewBalance)
        Wallet-->>GW: 200 OK: "Nạp tiền thành công, số dư mới..."
        GW-->>User: Hiển thị kết quả nạp tiền thành công

        Rabbit-)Notif: Consume WalletBalanceChangedEvent
        Notif->>Notif: Tạo thông báo "Nạp thành công +X đ vào ví"
        Notif->>Notif: Lưu vào Momo_NotificationDb
    end
```

---

## 3. Luồng Chuyển Tiền P2P MoMo Sang MoMo (P2P Money Transfer Flow)

Đây là luồng nghiệp vụ quan trọng nhất của MoMo: Khách hàng A chuyển tiền cho Khách hàng B qua Số Điện Thoại.
Luồng kết hợp:
1. **Idempotency Protection** qua Redis.
2. **gRPC inter-service communication** giữa Transaction Service và Wallet Service.
3. **Deadlock-Free Distributed Lock** trên 2 ví theo thứ tự chữ cái.
4. **RabbitMQ Event-Driven** thông báo số dư cho cả người gửi và người nhận.

```mermaid
sequenceDiagram
    autonumber
    actor Sender as Người Gửi (Alice)
    participant GW as API Gateway
    participant Txn as Transaction Service
    participant Redis as Redis Cache & Lock
    participant Wallet as Wallet Service (gRPC)
    participant DB_Txn as Momo_TransactionDb
    participant DB_Wallet as Momo_WalletDb
    participant Rabbit as RabbitMQ
    participant Notif as Notification Service
    actor Receiver as Người Nhận (Bob)

    Sender->>GW: POST /api/transactions/transfer<br/>Header: X-Idempotency-Key: UUID<br/>Body: { ReceiverPhone: "0907654321", Amount: 500000, Description: "Tra tien an" }
    GW->>Txn: Chuyển tiếp request kèm JWT

    Note over Txn,Redis: Bước 1: Kiểm tra Idempotency
    Txn->>Redis: SETNX idempotency:{Key}
    alt Key đã tồn tại (Bấm đúp nút)
        Redis-->>Txn: False
        Txn-->>Sender: 409 Conflict (DUPLICATE_TRANSACTION) hoặc trả kết quả đã cache
    end

    Note over Txn,Wallet: Bước 2: Xác minh ví người nhận qua gRPC
    Txn->>Wallet: gRPC: VerifyWallet(PhoneNumber="0907654321")
    Wallet-->>Txn: VerifyWalletResponse(Exists=true, ReceiverUserId, IsActive=true)

    Note over Txn,DB_Txn: Bước 3: Tạo giao dịch trạng thái PENDING
    Txn->>DB_Txn: Insert PaymentTransaction (Status=PENDING, Code="MOMO-xxx")

    Note over Txn,Wallet: Bước 4: Gọi gRPC thực thi chuyển tiền nguyên tử
    Txn->>Wallet: gRPC: TransferFunds(SenderId, ReceiverId, Amount=500000, TxnCode)

    Note over Wallet,Redis: Bước 5: Khóa phân tán cả 2 ví (Alphabetical Order)
    Wallet->>Redis: AcquireLocksInOrderAsync(["wallet_lock:Alice", "wallet_lock:Bob"])
    Redis-->>Wallet: Đã khóa cả 2 ví thành công (Không bị Deadlock)

    Wallet->>DB_Wallet: Bắt đầu Transaction CSDL (BeginTransaction)
    Wallet->>DB_Wallet: Đọc số dư Alice & Bob
    alt Số dư Alice < 500,000 VND
        Wallet->>DB_Wallet: Rollback Transaction
        Wallet->>Redis: Giải phóng khóa 2 ví
        Wallet-->>Txn: gRPC Response: Success=False, ErrorCode="INSUFFICIENT_BALANCE"
        Txn->>DB_Txn: Update PaymentTransaction (Status=FAILED)
        Txn-->>Sender: 400 Bad Request: "Số dư ví không đủ để chuyển"
    else Số dư hợp lệ
        Wallet->>DB_Wallet: Trừ tiền ví Alice: Balance -= 500,000
        Wallet->>DB_Wallet: Cộng tiền ví Bob: Balance += 500,000
        Wallet->>DB_Wallet: Insert Sổ cái Ledger Alice (Type='Debit')
        Wallet->>DB_Wallet: Insert Sổ cái Ledger Bob (Type='Credit')
        Wallet->>DB_Wallet: Commit Transaction
        Wallet->>Redis: Xóa Cache số dư cũ của cả Alice và Bob
        Wallet->>Redis: Giải phóng khóa 2 ví
        Wallet-->>Txn: gRPC Response: Success=True, SenderBalance, ReceiverBalance

        Note over Txn,DB_Txn: Bước 6: Cập nhật giao dịch thành công & Lưu kết quả Idempotency
        Txn->>DB_Txn: Update PaymentTransaction (Status=SUCCESS, CompletedAt=Now)
        Txn->>Redis: Lưu kết quả vào Cache Idempotency (TTL: 24h)
        
        Note over Txn,Rabbit: Bước 7: Bắn sự kiện bất đồng bộ
        Txn-)Rabbit: Publish TransactionCompletedEvent (TxnCode, AliceId, BobId, Amount=500000)

        Txn-->>GW: 200 OK + Chi tiết giao dịch thành công
        GW-->>Sender: Màn hình: "Chuyển tiền MoMo thành công!"

        Note over Rabbit,Notif: Bước 8: Worker xử lý thông báo ngầm
        Rabbit-)Notif: Consume TransactionCompletedEvent
        Notif->>Notif: Tạo thông báo cho Alice: "Bạn đã chuyển 500.000đ cho Bob"
        Notif->>Notif: Tạo thông báo cho Bob: "Bạn vừa nhận +500.000đ từ Alice"
        Notif->>Notif: Lưu 2 bản ghi vào Momo_NotificationDb
        Notif--)Receiver: (Push Notification tới điện thoại Bob)
    end
```

---

## 4. Luồng Rút Tiền Về Ngân Hàng (Withdrawal Flow)

Khách hàng rút tiền từ ví MoMo về tài khoản ngân hàng liên kết (VCB, Techcombank, MB, v.v.).

```mermaid
sequenceDiagram
    autonumber
    actor User as Khách hàng
    participant GW as API Gateway
    participant Wallet as Wallet Service
    participant Redis as Redis (Distributed Lock)
    participant DB_Wallet as Momo_WalletDb
    participant Rabbit as RabbitMQ
    participant Notif as Notification Service

    User->>GW: POST /api/wallet/withdraw (Amount, BankCode, BankAccountNumber)
    GW->>Wallet: Chuyển tiếp request
    Wallet->>Redis: AcquireLockAsync(lock:wallet_lock:{UserId})
    Wallet->>DB_Wallet: Kiểm tra số dư ví hiện tại
    alt Số dư ví < Số tiền rút
        Wallet->>Redis: Release Lock
        Wallet-->>User: 400 Bad Request: "Số dư không đủ để rút"
    else Số dư hợp lệ
        Wallet->>Wallet: Trừ tiền: Balance -= Amount
        Wallet->>DB_Wallet: Cập nhật Balance mới
        Wallet->>DB_Wallet: Insert Sổ cái (WalletLedger: Type='Debit')
        Wallet->>Redis: Invalidate Cache số dư
        Wallet->>Redis: Release Lock
        Wallet-)Rabbit: Publish WalletBalanceChangedEvent (Type='Debit')
        Wallet-->>User: 200 OK: "Rút tiền thành công về tài khoản ngân hàng"
        
        Rabbit-)Notif: Consume WalletBalanceChangedEvent
        Notif->>Notif: Lưu thông báo rút tiền vào Momo_NotificationDb
    end
```

---

## 5. Luồng Thanh Toán Đơn Hàng / Dịch Vụ (Merchant Payment Flow)

Khách hàng thanh toán tiền điện, nước, hóa đơn Shopee, Tiki, Lazada, hoặc mua vé máy bay thông qua MoMo.

```mermaid
sequenceDiagram
    autonumber
    actor Customer as Khách hàng
    participant GW as API Gateway
    participant Txn as Transaction Service
    participant Wallet as Wallet Service (gRPC)
    participant Rabbit as RabbitMQ
    participant Notif as Notification Service
    participant Merchant as Cổng Đối Tác / Merchant

    Customer->>GW: POST /api/transactions/payment (OrderId, MerchantId, Amount)
    GW->>Txn: Chuyển tiếp request
    Txn->>Txn: Tạo PaymentTransaction (Type=Payment, Status=PENDING)
    Txn->>Wallet: gRPC: WithdrawFunds(UserId, Amount, OrderId, MerchantId)
    alt Ví không đủ tiền
        Wallet-->>Txn: WithdrawFundsResponse(Success=false)
        Txn->>Txn: Update Status = FAILED
        Txn-->>Customer: Báo lỗi "Số dư không đủ thanh toán"
    else Trừ ví thành công
        Wallet-->>Txn: WithdrawFundsResponse(Success=true, NewBalance)
        Txn->>Txn: Update Status = SUCCESS
        Txn-)Rabbit: Publish TransactionCompletedEvent (Type=Payment)
        Txn-->>GW: 200 OK: "Thanh toán thành công đơn hàng"
        GW-->>Customer: Màn hình biên lai thanh toán
        
        Rabbit-)Notif: Consume Event -> Ghi nhận thông báo thanh toán
    end
```

---

## 6. Luồng Xử Lý Bất Đồng Bộ Với RabbitMQ (Event-Driven Notification Architecture)

Đảm bảo độ khả dụng cao và tách biệt hoàn toàn luồng giao dịch thanh toán khỏi luồng thông báo:

```mermaid
flowchart TD
    subgraph Publisher["Publisher (Transaction & Wallet Service)"]
        TxnSuccess["Giao dịch chuyển tiền / nạp tiền thành công"]
        CreateEvent["Đóng gói IntegrationEvent (JSON payload)"]
        PublishMsg["channel.BasicPublish() với DeliveryMode=2 (Persistent)"]
    end

    subgraph Broker["RabbitMQ Message Broker"]
        Exchange["Exchange: momo.events.exchange (Type: direct)"]
        BindingKey1{"RoutingKey: TransactionCompletedEvent"}
        BindingKey2{"RoutingKey: WalletBalanceChangedEvent"}
        Queue["Queue: notification_service_queue (Durable=True)"]
    end

    subgraph Consumer["Consumer (Notification Service Worker)"]
        Listen["AsyncEventingBasicConsumer.Received"]
        Scope["Tạo IServiceScopeFactory"]
        Dispatch["Gọi IIntegrationEventHandler.Handle()"]
        SaveDB[("Lưu vào Momo_NotificationDb")]
        Ack["channel.BasicAck() xác nhận hoàn tất"]
    end

    TxnSuccess --> CreateEvent --> PublishMsg --> Exchange
    Exchange --> BindingKey1 --> Queue
    Exchange --> BindingKey2 --> Queue
    Queue --> Listen --> Scope --> Dispatch --> SaveDB --> Ack
```
