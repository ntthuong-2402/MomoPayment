# Start MomoPayment via Docker Compose
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Starting MomoPayment Microservices Stack " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

docker compose up -d --build

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to start Docker Compose" -ForegroundColor Red
    exit 1
}

Write-Host "`nWaiting 10 seconds for containers to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

docker compose ps
Write-Host "`nSystem is running!" -ForegroundColor Green
Write-Host "Swagger UI: http://localhost:5000" -ForegroundColor Cyan
Write-Host "RabbitMQ UI: http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
