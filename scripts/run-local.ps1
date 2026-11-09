$ErrorActionPreference = "Stop"
$env:DOTNET_ROOT = "$HOME\.dotnet"
$env:PATH = "$HOME\.dotnet;$env:PATH"

dotnet restore "RaceDay.Api/RaceDay.Api.csproj"
dotnet restore "RaceDay.Tests/RaceDay.Tests.csproj"
dotnet build "RaceDay.Api/RaceDay.Api.csproj" --configuration Debug --no-restore
dotnet test "RaceDay.Tests/RaceDay.Tests.csproj" --nologo --no-restore

dotnet restore "RaceDay.Mvc/RaceDay.Mvc.csproj"
dotnet build "RaceDay.Mvc/RaceDay.Mvc.csproj" --configuration Debug --no-restore
