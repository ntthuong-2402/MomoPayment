# Clean build artifacts and Docker environment for MomoPayment
Write-Host "Stopping Docker containers and removing volumes..." -ForegroundColor Yellow
docker compose down -v --remove-orphans

Write-Host "Cleaning dotnet build artifacts (bin & obj)..." -ForegroundColor Yellow
Get-ChildItem -Recurse -Directory -Include bin, obj | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Clean completed successfully!" -ForegroundColor Green
