$ErrorActionPreference = "Stop"
$env:DOTNET_ROOT = "$HOME\.dotnet"
$env:PATH = "$HOME\.dotnet;$env:PATH"

dotnet restore "RaceDay.Api/RaceDay.Api.csproj"
dotnet restore "RaceDay.Tests/RaceDay.Tests.csproj"
dotnet build "RaceDay.Api/RaceDay.Api.csproj" --configuration Debug --no-restore
dotnet test "RaceDay.Tests/RaceDay.Tests.csproj" --nologo --no-restore
