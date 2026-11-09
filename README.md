# RaceDay — PROG6212 POE (Parts 1–3)

RaceDay is an ASP.NET Core event-management platform for running, walking and cycling events. Organisers create events, categories and results; participants browse events, enrol and view their results.

Part 3 is a **separate ASP.NET Core MVC application** that talks to the Part 2 REST API over HTTP. The MVC app never opens a SQL Server connection and never talks to Azure Blob Storage directly. Authentication, persistence, ownership rules and media validation stay in the API.

**Repository:** <https://github.com/Killmanga-AI/Prograceday>  
**Branch:** `part-3-mvc-webapp` (branched from `part-2-rest-api`)

## Architecture

```text
Browser
  │  HTTP + session cookie + antiforgery token
  ▼
RaceDay.Mvc  ──HTTP/JSON + bearer token──►  RaceDay.Api  ──EF Core──►  SQL Server
  │                                             │
  └── server-side session (JWT, user, role)      └── IBlobStorageService
                                                     ├── Azure Blob Storage (configured)
                                                     └── local App_Data/media (default/dev)
```

| Project | Purpose |
|---|---|
| `RaceDay.Api` | ASP.NET Core 8 REST API, EF Core, JWT auth, uploads, Swagger |
| `RaceDay.Mvc` | ASP.NET Core 8 MVC frontend, typed API client, session auth |
| `RaceDay.Tests` | API integration tests (WebApplicationFactory) |
| `RaceDay.Mvc.Tests` | MVC integration tests against an in-process stub API |

The MVC project has no `ProjectReference` to the API and no EF Core or Azure packages. `RaceDayApiClient` is the only integration point.

## Features

### Participant
- Register and sign in through the API; logout clears the server-side session.
- Browse and filter public events by type, date and search text.
- View event details, categories, distance, capacity and remaining places.
- Enrol in an open category and cancel an enrolment that has no result.
- See enrolments and results (event date, category, finish time, position out of finishers).
- Update profile details and upload a JPEG/PNG/WebP profile picture.

### Organiser
- Dashboard with event, enrolment and result totals plus recent activity.
- Create, edit and delete owned events, including a banner upload.
- Add, edit and delete categories with capacity and distance.
- View enrolments per event and capture results.
- Uploads and event changes are rejected by the API when the organiser does not own the event.

### Cross-cutting
- MVC session stores the JWT, user ID, name, email and role; idle sessions expire.
- Role-aware navigation plus server-side `SessionAuthorize` checks; the API re-checks every call.
- Antiforgery validation on every MVC POST.
- Friendly 403/404/500 status pages; security headers and no-store caching for authenticated pages.
- Upload validation on both sides: allowed image types, signature check and a 5 MB limit.
- Responsive, keyboard-accessible Bootstrap UI.

## Technology

- .NET 8, ASP.NET Core MVC and minimal hosting
- Entity Framework Core 8 with SQL Server (InMemory for tests)
- JWT bearer authentication and role authorisation
- Swashbuckle / OpenAPI
- Azure.Storage.Blobs, with a local filesystem provider for development
- xUnit + `WebApplicationFactory`
- Docker multi-stage builds and Docker Compose
- GitHub Actions

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB is fine on Windows) for local runs
- Docker Desktop for the container stack (optional)
- An Azure Storage account only if you want to exercise the Azure media provider

## Run locally

### 1. Configure the API

Create `RaceDay.Api/appsettings.Development.json` from `appsettings.Development.json.example`, then store secrets with user secrets (never commit them):

```powershell
dotnet user-secrets init --project RaceDay.Api/RaceDay.Api.csproj
dotnet user-secrets set "Jwt:Key" "<random secret of at least 32 characters>" --project RaceDay.Api/RaceDay.Api.csproj
```

The default media provider is `Local`, which writes to `RaceDay.Api/App_Data/media` and serves it from `/media`. Uploads work without Azure credentials. To use Azure instead:

```powershell
dotnet user-secrets set "Media:Provider" "Azure" --project RaceDay.Api/RaceDay.Api.csproj
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your connection string>" --project RaceDay.Api/RaceDay.Api.csproj
dotnet user-secrets set "AzureBlobStorage:ContainerName" "raceday-media" --project RaceDay.Api/RaceDay.Api.csproj
```

The Azure provider creates the container with blob-level public read access so browsers can display images. For a private deployment, replace that with short-lived SAS URLs.

### 2. Apply migrations

```powershell
dotnet tool restore
dotnet ef database update --project RaceDay.Api/RaceDay.Api.csproj --startup-project RaceDay.Api/RaceDay.Api.csproj
```

`Database:AutoMigrate` can apply migrations at startup; it is enabled in Compose and off by default locally.

### 3. Start both applications

```powershell
# Terminal 1 — API on http://localhost:5171 and https://localhost:7171
dotnet run --project RaceDay.Api/RaceDay.Api.csproj

# Terminal 2 — MVC (uses http://localhost:5171/ by default)
dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj
```

Open the MVC URL printed by the host. Swagger is at `https://localhost:7171/swagger`; the API health endpoint is `http://localhost:5171/health`.

If the API runs elsewhere:

```powershell
$env:RaceDayApi__BaseUrl = "https://localhost:7171/"
dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj
```

## Configuration

### API

| Key | Default | Notes |
|---|---|---|
| `ConnectionStrings:RaceDay` | LocalDB | SQL Server connection string |
| `Jwt:Key` | — | Required, 32+ characters; use user secrets or env vars |
| `Jwt:Issuer` / `Jwt:Audience` | `RaceDay` / `RaceDayClients` | Token validation |
| `Media:Provider` | `Local` | `Local` or `Azure` |
| `Media:LocalPath` | `App_Data/media` | Local provider root |
| `Media:PublicBaseUrl` | — | Absolute base for returned image URLs |
| `AzureBlobStorage:ConnectionString` | — | Required for the Azure provider |
| `Database:AutoMigrate` | `false` | Apply migrations at startup |
| `Https:Redirect` | `true` | Disable behind an HTTP-only proxy |
| `Swagger:Enabled` | `false` | Also enabled in Development/Testing |

### MVC

| Key | Default | Notes |
|---|---|---|
| `RaceDayApi:BaseUrl` | `http://localhost:5171/` | API root URL |
| `RaceDayApi:TimeoutSeconds` | `30` | HTTP client timeout |
| `Session:IdleMinutes` | `30` | Session idle timeout |
| `Session:SecureCookies` | `true` outside Development | Set `false` only for plain-HTTP local/Compose use |
| `Https:Redirect` | `true` | Disable for plain-HTTP containers |

Environment-variable form uses double underscores, e.g. `RaceDayApi__BaseUrl`.

## Tests

Both suites were run on 2026-11-09 against the final code:

| Command | Result |
|---|---|
| `dotnet build RaceDay.sln -c Release` | Build succeeded, 0 warnings, 0 errors |
| `dotnet test RaceDay.Tests/RaceDay.Tests.csproj -c Release` | **29 passed**, 0 failed |
| `dotnet test RaceDay.Mvc.Tests/RaceDay.Mvc.Tests.csproj -c Release` | **40 passed**, 0 failed |

What the tests cover:

- **API:** registration/login validation, JWT roles, public event and category browsing, organiser ownership scoping, enrolment capacity, result capture, participant results, upload type/size validation, Swagger completeness, health endpoint, local media storage.
- **MVC:** anonymous and authenticated routing, role separation (participant vs organiser), session creation/expiry/logout, antiforgery rejection, upload validation before the API call, organiser CRUD flows, participant browse/enrol/cancel/results, profile updates, friendly error pages and cache headers.

The MVC tests start a real Kestrel **stub API** on a loopback port and point the MVC app at it. Real HTTP, JSON serialization, multipart uploads and cookies are exercised; API business logic is covered by the API suite. Tests do not prove Azure Blob Storage or a live SQL Server migration — those require credentials/infrastructure (CI performs the SQL migration check).

See [`docs/TEST_RESULTS.md`](docs/TEST_RESULTS.md) for the exact commands and output summary.

## Docker Compose

The stack is SQL Server 2022 + API + MVC on an internal network with named volumes and health checks.

```bash
cp .env.example .env      # then edit both secrets
docker compose up --build
```

- MVC: <http://localhost:8080>
- API/Swagger: <http://localhost:8081/swagger>
- SQL Server is not published to the host by default.

The API waits for SQL Server's health check and applies migrations (with retries). Uploaded images persist in the `raceday-media` volume; database files persist in `mssql-data`. Compose serves plain HTTP for local use, so `Session__SecureCookies` is explicitly disabled; put TLS in front before exposing it publicly.

Useful commands:

```bash
docker compose config          # validate the model and rendered environment
docker compose ps
docker compose logs -f raceday-api
docker compose down            # keep volumes
docker compose down -v         # also delete database and media volumes
```

**Verification status:** the Compose model is validated with `docker compose config`. The Docker daemon was not running on the authoring machine, so the images were **not** built or run there. The CI workflow builds both images on every push.

## API endpoints

| Method | Endpoint | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| GET | `/api/events` | Public |
| GET | `/api/events/{id}` | Public |
| GET | `/api/events/{id}/categories` | Public |
| POST | `/api/events` | Organiser |
| PUT / DELETE | `/api/events/{id}` | Owning organiser |
| GET | `/api/organiser/dashboard` | Organiser |
| GET | `/api/organiser/events` | Organiser |
| POST | `/api/events/{eventId}/categories` | Owning organiser |
| PUT / DELETE | `/api/categories/{id}` | Owning organiser |
| GET | `/api/events/{id}/enrolments` | Owning organiser |
| POST | `/api/categories/{id}/enrolments` | Participant |
| GET | `/api/users/me/enrolments` | Participant |
| DELETE | `/api/enrolments/{id}` | Enrolment owner |
| POST | `/api/enrolments/{id}/results` | Owning organiser |
| GET | `/api/users/me/results` | Participant |
| GET / PUT | `/api/users/me` | Authenticated |
| POST | `/api/events/{id}/banner` | Owning organiser |
| POST | `/api/users/me/profile-picture` | Authenticated |
| GET | `/health` | Public |

## Security notes

- The JWT is kept in server-side session, never in browser storage.
- Cookies are HttpOnly and SameSite=Lax; enable secure cookies behind TLS.
- Every MVC POST validates an antiforgery token.
- The API is authoritative for roles and ownership; hiding a link is not access control.
- Uploads are limited to JPEG, PNG and WebP, checked by declared type and file signature, with a 5 MB cap.
- Secrets are read from user secrets/environment variables; no credentials are committed.
- Public registration currently allows either role, matching Part 2. A production system should add organiser approval.
- Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, a restrictive CSP) are applied globally.

## CI

`.github/workflows/dotnet.yml` runs three jobs on every push and pull request:

1. **Build and test** — restore and build the whole solution, run both xUnit suites, upload TRX results.
2. **SQL Server migration check** — start SQL Server 2022, wait for readiness, `dotnet ef database update`, assert the schema and migration history.
3. **Docker image builds** — build the API and MVC images.

## Repository layout

```text
Prograceday/
├── RaceDay.Api/              # REST API
│   ├── Controllers/          # Auth, events, categories, enrolments, users, media
│   ├── Data/                 # EF Core context
│   ├── Migrations/
│   ├── Models/ Dtos/         # Entities and request contracts
│   ├── Services/             # JWT, Azure and local blob storage
│   └── Dockerfile
├── RaceDay.Mvc/              # MVC frontend
│   ├── Controllers/ Filters/ Models/ Services/ Views/ wwwroot/
│   └── Dockerfile
├── RaceDay.Tests/            # API integration tests
├── RaceDay.Mvc.Tests/        # MVC integration tests + stub API
├── docs/                     # Rubric mapping, test results, demo script
├── .github/workflows/        # CI
├── docker-compose.yml
└── RaceDay.sln
```

## Documentation

- [`docs/PART3_IMPLEMENTATION.md`](docs/PART3_IMPLEMENTATION.md) — design decisions and request flows
- [`docs/PART3_RUBRIC_MAPPING.md`](docs/PART3_RUBRIC_MAPPING.md) — requirement-to-code checklist
- [`docs/TEST_RESULTS.md`](docs/TEST_RESULTS.md) — measured build/test results
- [`docs/SUBMISSION_CHECKLIST.md`](docs/SUBMISSION_CHECKLIST.md) — pre-submission steps
- [`docs/DEMO_SCRIPT.md`](docs/DEMO_SCRIPT.md) — video walkthrough plan

## Git history

Work continues on `part-3-mvc-webapp`, branched from `part-2-rest-api`. The branch contains the original Part 3 import plus incremental commits for the typed API client, session authentication, upload validation, status pages, local media provider, startup health/migrations, tests, Docker, CI and documentation. Use `git log --oneline --graph` to review it.

## Demonstration video

_Add the unlisted YouTube URL here before submitting. See `docs/DEMO_SCRIPT.md` for the planned walkthrough._ This placeholder must not be submitted as a real link.

## Known limitations

- The Docker stack was not executed on the authoring machine (daemon unavailable); CI builds the images and the Compose model validates.
- Azure Blob Storage requires the student's own storage account and was not exercised with live credentials; the local provider covers the same contract in development/tests.
- Public registration allows either role; organiser approval is future work.
- No password-reset or email-verification flow.
- The demo video still needs to be recorded and linked.

## AI use disclosure

Update this statement to match your institution's rules and your own contribution. AI assistance was used for scaffolding, integration planning, documentation and review. Review, run, understand and be able to explain all submitted code. Do not claim independently authored work if AI assistance was used.
