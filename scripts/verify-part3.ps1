$ErrorActionPreference = "Stop"

Write-Host "Restoring RaceDay solution..." -ForegroundColor Cyan
dotnet restore "RaceDay.sln"

Write-Host "Building RaceDay solution..." -ForegroundColor Cyan
dotnet build "RaceDay.sln" --configuration Release --no-restore

Write-Host "Running API integration tests..." -ForegroundColor Cyan
dotnet test "RaceDay.Tests/RaceDay.Tests.csproj" --configuration Release --no-build --no-restore

Write-Host "Running MVC integration tests..." -ForegroundColor Cyan
dotnet test "RaceDay.Mvc.Tests/RaceDay.Mvc.Tests.csproj" --configuration Release --no-build --no-restore

Write-Host "Validating the Compose model..." -ForegroundColor Cyan
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created .env from .env.example. Change the placeholder secrets before starting the stack." -ForegroundColor Yellow
}
docker compose --env-file ".env" config --quiet

Write-Host "Checking the Docker daemon..." -ForegroundColor Cyan
docker info *> $null
if ($LASTEXITCODE -eq 0) {
    docker compose --env-file ".env" build
    Write-Host "Docker images built. Run 'docker compose up' to start the stack." -ForegroundColor Green
}
else {
    Write-Host "Docker daemon is not running; skipped image builds." -ForegroundColor Yellow
}

Write-Host "All available verification commands completed." -ForegroundColor Green
