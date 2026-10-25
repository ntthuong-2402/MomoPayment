# Automated End-to-End API Flow Test for MomoPayment
$baseUrl = "http://localhost:5000"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Running MomoPayment E2E Integration Test  " -ForegroundColor Cyan
Write-Host " Target Gateway: $baseUrl                 " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Login Alice
Write-Host "`n[1/5] Logging in as Alice (0901234567)..." -ForegroundColor Yellow
$aliceLoginBody = @{
    phoneNumber = "0901234567"
    password = "Password123@"
} | ConvertTo-Json

try {
    $aliceResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $aliceLoginBody -ContentType "application/json"
    $aliceToken = $aliceResp.data.accessToken
    Write-Host "-> Success! Alice Logged In. Name: $($aliceResp.data.user.fullName)" -ForegroundColor Green
} catch {
    Write-Host "-> FAILED to log in Alice: $_" -ForegroundColor Red
    exit 1
}

# 2. Check Alice Balance
Write-Host "`n[2/5] Checking Alice's initial balance..." -ForegroundColor Yellow
try {
    $aliceBal = Invoke-RestMethod -Uri "$baseUrl/api/wallet/balance" -Method Get -Headers @{ Authorization = "Bearer $aliceToken" }
    Write-Host "-> Alice Balance: $($aliceBal.data.balance.ToString('N0')) VND" -ForegroundColor Green
} catch {
    Write-Host "-> FAILED to get Alice's balance: $_" -ForegroundColor Red
    exit 1
}

# 3. Lookup Bob
Write-Host "`n[3/5] Looking up Bob's info by phone (0907654321)..." -ForegroundColor Yellow
try {
    $bobLookup = Invoke-RestMethod -Uri "$baseUrl/api/users/lookup?phone=0907654321" -Method Get -Headers @{ Authorization = "Bearer $aliceToken" }
    Write-Host "-> Recipient: $($bobLookup.data.fullName) ($($bobLookup.data.maskedPhoneNumber))" -ForegroundColor Green
} catch {
    Write-Host "-> FAILED to lookup Bob: $_" -ForegroundColor Red
    exit 1
}

# 4. Perform Transfer
$idempotencyKey = [System.Guid]::NewGuid().ToString()
Write-Host "`n[4/5] Transferring 500,000 VND to Bob with Idempotency Key: $idempotencyKey..." -ForegroundColor Yellow
$transferBody = @{
    receiverPhone = "0907654321"
    amount = 500000
    description = "Test transfer via automation script"
} | ConvertTo-Json

try {
    $transferResp = Invoke-RestMethod -Uri "$baseUrl/api/transactions/transfer" -Method Post -Body $transferBody -ContentType "application/json" -Headers @{
        Authorization = "Bearer $aliceToken"
        "X-Idempotency-Key" = $idempotencyKey
    }
    Write-Host "-> Transfer Succeeded! Txn Code: $($transferResp.data.transactionCode)" -ForegroundColor Green
} catch {
    Write-Host "-> FAILED transfer: $_" -ForegroundColor Red
    exit 1
}

# 5. Check Bob's New Balance & Notification
Write-Host "`n[5/5] Logging in as Bob to verify balance and RabbitMQ notification..." -ForegroundColor Yellow
$bobLoginBody = @{
    phoneNumber = "0907654321"
    password = "Password123@"
} | ConvertTo-Json

try {
    $bobResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $bobLoginBody -ContentType "application/json"
    $bobToken = $bobResp.data.accessToken

    $bobBal = Invoke-RestMethod -Uri "$baseUrl/api/wallet/balance" -Method Get -Headers @{ Authorization = "Bearer $bobToken" }
    Write-Host "-> Bob New Balance: $($bobBal.data.balance.ToString('N0')) VND" -ForegroundColor Green

    Start-Sleep -Seconds 2
    $bobNotifs = Invoke-RestMethod -Uri "$baseUrl/api/notifications" -Method Get -Headers @{ Authorization = "Bearer $bobToken" }
    Write-Host "-> Bob Notifications Count: $($bobNotifs.data.totalCount)" -ForegroundColor Green
    if ($bobNotifs.data.items.Count -gt 0) {
        Write-Host "-> Latest Notification: $($bobNotifs.data.items[0].title) - $($bobNotifs.data.items[0].content)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "-> FAILED verification for Bob: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n==========================================" -ForegroundColor Green
Write-Host " ALL TESTS PASSED! MICROSERVICES ARE HEALTHY " -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
