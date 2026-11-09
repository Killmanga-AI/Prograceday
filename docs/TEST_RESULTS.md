# Test results

Measured on 2026-11-09 on Windows with .NET SDK 10.0.401 targeting `net8.0` (runtime 8.0.32).

## Build

```text
dotnet build RaceDay.sln --configuration Release
Build succeeded.
0 warnings
0 errors
```

Projects built: `RaceDay.Api`, `RaceDay.Mvc`, `RaceDay.Tests`, `RaceDay.Mvc.Tests`.

## API tests

```text
dotnet test RaceDay.Tests/RaceDay.Tests.csproj --configuration Release
Passed! - Failed: 0, Passed: 29, Skipped: 0, Total: 29
```

Coverage includes authentication and validation, JWT roles, public browse endpoints, organiser ownership scoping, enrolment capacity, result recording, participant results, upload type/size checks, Swagger operation coverage, health endpoint and local media storage.

## MVC tests

```text
dotnet test RaceDay.Mvc.Tests/RaceDay.Mvc.Tests.csproj --configuration Release
Passed! - Failed: 0, Passed: 40, Skipped: 0, Total: 40
```

The MVC suite hosts the real application with `WebApplicationFactory` and a Kestrel stub API on a random loopback port. It exercises:

- anonymous/login/register/logout and session expiry;
- participant vs organiser authorisation, including direct URL attempts;
- antiforgery rejection on POSTs;
- upload validation before the API call and successful multipart uploads;
- event CRUD, categories, enrolments and results;
- profile updates;
- 404/500 status pages and `no-store` headers on authenticated pages.

## Not covered by these runs

| Area | Status |
|---|---|
| SQL Server migration | Not run locally; the CI workflow applies migrations to SQL Server 2022 and asserts the schema |
| Docker image build/run | Not run locally (daemon unavailable); CI builds both images |
| Azure Blob Storage | Not exercised without a storage account; local provider tested instead |
| Browser/visual review | Pages are covered by HTML assertions; no screenshot review recorded |

Do not present the unchecked rows as passes.
