$ErrorActionPreference = "Stop"

Write-Host "Restoring RaceDay solution..." -ForegroundColor Cyan
dotnet restore "RaceDay.sln"

Write-Host "Building RaceDay solution..." -ForegroundColor Cyan
dotnet build "RaceDay.sln" --configuration Release --no-restore

Write-Host "Running API integration tests..." -ForegroundColor Cyan
dotnet test "RaceDay.Tests/RaceDay.Tests.csproj" --configuration Release --no-build --no-restore

Write-Host "Building MVC Docker image..." -ForegroundColor Cyan
docker build -t raceday-mvc .

Write-Host "All available verification commands completed." -ForegroundColor Green
