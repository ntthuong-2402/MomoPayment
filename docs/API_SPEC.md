# Tài Liệu Đặc Tả API & Hướng Dẫn Gọi Test (API Specification & cURL)

Toàn bộ các API có thể gọi thông qua **API Gateway** tại cổng `http://localhost:5000` hoặc gọi trực tiếp vào từng service.

---

## 1. Danh Sách Tài Khoản Thử Nghiệm Có Sẵn (Seeded Accounts)

CSDL tự động tạo sẵn 3 tài khoản thử nghiệm với mật khẩu chung: `Password123@`:

| Họ và tên | Số điện thoại | Số dư khởi tạo | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Nguyễn Văn An (Alice)** | `0901234567` | **5,000,000 VND** | Dùng làm người gửi tiền |
| **Trần Thị Bình (Bob)** | `0907654321` | **2,000,000 VND** | Dùng làm người nhận tiền |
| **Lê Văn Cường (Charlie)** | `0912345678` | **1,000,000 VND** | Khách hàng thông thường |

---

## 2. API Quản Lý Danh Tính (Identity & User Service)

### 2.1. Đăng nhập (Login)
- **Method:** `POST`
- **Endpoint:** `http://localhost:5000/api/auth/login`
- **Request Body:**
  ```json
  {
    "phoneNumber": "0901234567",
    "password": "Password123@"
  }
  ```
- **Response Success (200 OK):**
  ```json
  {
    "success": true,
    "message": "Đăng nhập thành công",
    "data": {
      "accessToken": "eyJhbGciOi...",
      "tokenType": "Bearer",
      "expiresIn": 86400,
      "user": {
        "id": "11111111-1111-1111-1111-111111111111",
        "phoneNumber": "0901234567",
        "fullName": "Nguyễn Văn An",
        "email": "an.nguyen@example.com",
        "role": "User"
      }
    }
  }
  ```
- **cURL Command:**
  ```bash
  curl -X POST http://localhost:5000/api/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"phoneNumber\":\"0901234567\",\"password\":\"Password123@\"}"
  ```

---

### 2.2. Đăng ký tài khoản mới (Register)
- **Method:** `POST`
- **Endpoint:** `http://localhost:5000/api/auth/register`
- **Request Body:**
  ```json
  {
    "phoneNumber": "0988776655",
    "password": "Password123@",
    "fullName": "Phạm Minh Hoàng",
    "email": "hoang.pham@example.com"
  }
  ```

---

### 2.3. Tra cứu người nhận qua Số Điện Thoại (Lookup User by Phone)
- **Method:** `GET`
- **Endpoint:** `http://localhost:5000/api/users/lookup?phone=0907654321`
- **Header:** `Authorization: Bearer <TOKEN_ALICE>`
- **Response Success (200 OK):**
  ```json
  {
    "success": true,
    "message": "Tìm thấy thông tin người nhận",
    "data": {
      "id": "22222222-2222-2222-2222-222222222222",
      "phoneNumber": "0907654321",
      "maskedPhoneNumber": "090****321",
      "fullName": "Trần Thị Bình"
    }
  }
  ```

---

## 3. API Quản Lý Ví & Số Dư (Wallet Service)

### 3.1. Truy vấn số dư ví (Get Wallet Balance)
- **Method:** `GET`
- **Endpoint:** `http://localhost:5000/api/wallet/balance`
- **Header:** `Authorization: Bearer <TOKEN_ALICE>`
- **Response Success (200 OK):**
  ```json
  {
    "success": true,
    "message": "Lấy số dư ví thành công",
    "data": {
      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "userId": "11111111-1111-1111-1111-111111111111",
      "phoneNumber": "0901234567",
      "balance": 5000000.00,
      "currency": "VND",
      "status": 1
    }
  }
  ```

---

### 3.2. Nạp tiền vào ví MoMo (Deposit)
- **Method:** `POST`
- **Endpoint:** `http://localhost:5000/api/wallet/deposit`
- **Header:** `Authorization: Bearer <TOKEN_ALICE>`
- **Request Body:**
  ```json
  {
    "amount": 200000,
    "paymentSource": "Vietcombank (*1234)",
    "description": "Nạp tiền tiêu vặt"
  }
  ```

---

### 3.3. Rút tiền về ngân hàng (Withdraw)
- **Method:** `POST`
- **Endpoint:** `http://localhost:5000/api/wallet/withdraw`
- **Header:** `Authorization: Bearer <TOKEN_ALICE>`
- **Request Body:**
  ```json
  {
    "amount": 100000,
    "bankCode": "VCB",
    "bankAccountNumber": "0071000123456",
    "description": "Rút tiền về thẻ ATM"
  }
  ```

---

### 3.4. Xem sổ cái biến động số dư ví (Get Wallet Ledger)
- **Method:** `GET`
- **Endpoint:** `http://localhost:5000/api/wallet/ledger?page=1&pageSize=10`
- **Header:** `Authorization: Bearer <TOKEN_ALICE>`

---

## 4. API Giao Dịch & Chuyển Tiền (Transaction Service)

### 4.1. Chuyển tiền P2P MoMo sang MoMo (Transfer)
- **Method:** `POST`
- **Endpoint:** `http://localhost:5000/api/transactions/transfer`
- **Headers:**
  - `Authorization: Bearer <TOKEN_ALICE>`
  - `X-Idempotency-Key: e1a3b90a-1122-3344-5566-778899aabbcc`
- **Request Body:**
  ```json
  {
    "receiverPhone": "0907654321",
    "amount": 500000,
    "description": "Chuyển tiền ăn tối hôm qua nhé"
  }
  ```
- **Response Success (200 OK):**
  ```json
  {
    "success": true,
    "message": "Chuyển thành công 500,000 VND đến 0907654321",
    "data": {
      "id": "7b0492cb-5095-4eb8-b99b-3a5c2d73356e",
      "transactionCode": "MOMO-20261024151030-9E1A2B",
      "senderUserId": "11111111-1111-1111-1111-111111111111",
      "senderPhone": "0901234567",
      "receiverUserId": "22222222-2222-2222-2222-222222222222",
      "receiverPhone": "0907654321",
      "amount": 500000.00,
      "fee": 0.00,
      "netAmount": 500000.00,
      "type": 3,
      "status": 2,
      "description": "Chuyển tiền ăn tối hôm qua nhé",
      "createdAt": "2026-10-24T08:10:30Z",
      "completedAt": "2026-10-24T08:10:31Z"
    }
  }
  ```

---

### 4.2. Thanh toán đơn hàng / Hóa đơn dịch vụ (Payment)
- **Method:** `POST`
- **Endpoint:** `http://localhost:5000/api/transactions/payment`
- **Header:** `Authorization: Bearer <TOKEN_ALICE>`
- **Request Body:**
  ```json
  {
    "orderId": "DH-2026-9999",
    "merchantId": "SHOPEE_VN",
    "amount": 150000,
    "description": "Thanh toán đơn hàng Shopee"
  }
  ```

---

### 4.3. Xem lịch sử giao dịch (Transaction History)
- **Method:** `GET`
- **Endpoint:** `http://localhost:5000/api/transactions/history?page=1&pageSize=10`
- **Header:** `Authorization: Bearer <TOKEN_ALICE>`

---

## 5. API Hộp Thư Thông Báo Biến Động Số Dư (Notification Service)

### 5.1. Xem danh sách thông báo (Get Notifications)
- **Method:** `GET`
- **Endpoint:** `http://localhost:5000/api/notifications?page=1&pageSize=10`
- **Header:** `Authorization: Bearer <TOKEN_BOB>`
- **Response Success (200 OK):**
  ```json
  {
    "success": true,
    "data": {
      "items": [
        {
          "id": "c1a2d3e4-1234-5678-9abc-def012345678",
          "userId": "22222222-2222-2222-2222-222222222222",
          "title": "Biến động số dư: Nhận tiền MoMo",
          "content": "Bạn vừa nhận được +500,000 VND từ 0901234567. Lời nhắn: Chuyển tiền ăn tối hôm qua nhé",
          "type": 1,
          "referenceTransactionCode": "MOMO-20261024151030-9E1A2B",
          "amount": 500000.00,
          "isRead": false,
          "createdAt": "2026-10-24T08:10:31Z"
        }
      ],
      "totalCount": 1,
      "pageIndex": 1,
      "pageSize": 10
    }
  }
  ```

---

### 5.2. Đếm số thông báo chưa đọc (Unread Count)
- **Method:** `GET`
- **Endpoint:** `http://localhost:5000/api/notifications/unread-count`
- **Header:** `Authorization: Bearer <TOKEN_BOB>`
