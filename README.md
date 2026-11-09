# RaceDay — PROG6212 POE (Parts 1–3)

RaceDay is an ASP.NET Core event management platform for running, walking and cycling events. Organisers create and manage events, categories, enrolments and results. Participants browse events, register for categories, track their enrolments and view their race results.

**Part 3 architecture:** the ASP.NET Core MVC application is a presentation layer that communicates with the Part 2 REST API over HTTP. The MVC application does not connect directly to SQL Server or Azure Blob Storage. Authentication, data access, ownership checks and business rules remain in the API.

## Solution structure

```text
Prograceday/
├── RaceDay.Api/              # ASP.NET Core 8 REST API
│   ├── Controllers/          # Auth, events, users, categories, media
│   ├── Data/                 # EF Core DbContext
│   ├── Dtos/                 # API request contracts
│   ├── Migrations/           # Database migrations
│   ├── Models/               # EF Core entities
│   └── Services/             # JWT and Azure Blob Storage services
├── RaceDay.Tests/            # API integration tests
├── RaceDay.Mvc/              # ASP.NET Core MVC frontend
│   ├── Controllers/
│   ├── Filters/              # Session and role protection
│   ├── Models/
│   ├── Services/             # Typed API client
│   ├── Views/
│   └── wwwroot/
├── docs/                     # Original planning and Part 3 documentation
├── .github/workflows/        # API tests, MVC build and Docker build
├── Dockerfile                # MVC container
└── docker-compose.yml
```

## Main functionality

### Shared
- Participant and organiser registration and login.
- JWT-based API authentication.
- Server-side MVC session stores the token, user ID, name, email and role.
- Role-specific navigation and protected controller actions.
- Logout clears the session.

### Participant
- Browse and filter upcoming events.
- View event details, categories, distance, date and location.
- Enrol in an available category.
- View and cancel personal enrolments.
- View recorded results, event date, category, finish time and position out of total finishers.
- Update profile details and upload a profile picture.

### Organiser
- Dashboard with event, enrolment and result totals.
- Create, update and delete owned events.
- Upload event banners.
- Add, update and delete categories.
- View event enrolments.
- Capture race results.

### API and storage
- SQL Server persistence through EF Core.
- JWT authentication and role-based authorisation.
- Organiser ownership checks on event, category, enrolment and result operations.
- Azure Blob Storage upload endpoints for event banners and profile pictures.
- Image type allow-list (JPEG, PNG, WebP) and 5 MB maximum upload size.
- API validates the user/event relationship before saving uploaded media.

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB is suitable for Windows development)
- Visual Studio 2022 or another .NET 8 compatible IDE
- Azure Storage account for live image uploads (optional while developing the non-upload features)
- Docker Desktop for container execution

## 1. Configure the API

Create `RaceDay.Api/appsettings.Development.json` using `RaceDay.Api/appsettings.Development.json.example` as a template. Configure your own SQL Server connection string.

Set the JWT signing key using user secrets. Do not commit secrets:

```powershell
dotnet user-secrets init --project RaceDay.Api/RaceDay.Api.csproj
dotnet user-secrets set "Jwt:Key" "replace-with-a-random-secret-at-least-32-characters" --project RaceDay.Api/RaceDay.Api.csproj
```

For Azure Blob Storage, configure the connection string and container name as user secrets:

```powershell
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "YOUR_AZURE_STORAGE_CONNECTION_STRING" --project RaceDay.Api/RaceDay.Api.csproj
dotnet user-secrets set "AzureBlobStorage:ContainerName" "raceday-media" --project RaceDay.Api/RaceDay.Api.csproj
```

The image service creates the container if it does not exist and configures blob-level public read access so event banners and profile pictures can be displayed by browsers. The storage account must permit anonymous blob access. If your organisation prohibits public blob access, replace this with short-lived SAS URL generation before deployment. Never put the connection string in the MVC application.

## 2. Apply the database migration

Back up any existing database before applying migrations. The new Part 3 migration adds nullable `BannerImageUrl` to `Events` and `ProfilePictureUrl` to `Users`.

Install EF tooling if required:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
```

Apply all migrations:

```powershell
dotnet ef database update --project RaceDay.Api/RaceDay.Api.csproj --startup-project RaceDay.Api/RaceDay.Api.csproj
```

Do not run `docs/database_schema.sql` against an existing database: that planning script drops and recreates the original tables. If your database was created manually from that script rather than EF migrations, back it up and baseline it deliberately before applying migrations.

## 3. Run the API

The included launch profile uses HTTP port `5171` and HTTPS port `7171`.

```powershell
dotnet run --project RaceDay.Api/RaceDay.Api.csproj
```

API Swagger: `https://localhost:7171/swagger` (or the HTTP equivalent).

Health check: `http://localhost:5171/health`.

## 4. Run the MVC application

The MVC app defaults to `http://localhost:5171/` for the API. This matches the API launch profile above.

In a second terminal:

```powershell
dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj
```

Open the MVC URL printed by the .NET host (normally `https://localhost:7xxx` or `http://localhost:5xxx`).

If the API runs at a different address, set the MVC configuration value:

```powershell
$env:RaceDayApi__BaseUrl = "https://localhost:7171/"
dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj
```

When using HTTPS locally, ensure the development certificate is trusted. If the MVC container cannot validate a local self-signed API certificate, use a trusted development certificate or configure a local HTTP API endpoint for development only.

## 5. Run tests and builds

Run API integration tests:

```powershell
dotnet test RaceDay.Tests/RaceDay.Tests.csproj --configuration Release
```

Build the MVC project:

```powershell
dotnet build RaceDay.Mvc/RaceDay.Mvc.csproj --configuration Release
```

The GitHub Actions workflow restores, builds and tests the API, builds the MVC application, and builds the MVC Docker image. The tests use an isolated EF Core InMemory database; they do not replace a SQL Server migration check or an Azure Storage integration test.

## 6. Run the MVC Docker container

Build:

```bash
docker build -t raceday-mvc .
```

Run the container and point it to an API reachable from the container:

```bash
docker run --rm -p 8080:8080 \
  -e RaceDayApi__BaseUrl="http://host.docker.internal:5171/" \
  raceday-mvc
```

Then open `http://localhost:8080`.

Alternatively:

```bash
docker compose up --build
```

Set `RACEDAY_API_URL` before starting Compose if the API is hosted somewhere other than the host's port 5171. The Docker image contains the MVC application only; the API and SQL Server remain separately hosted services.

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
| GET / PUT | `/api/users/me` | Authenticated user |
| POST | `/api/events/{id}/banner` | Owning organiser |
| POST | `/api/users/me/profile-picture` | Authenticated user |

## Security and design notes

- MVC stores the JWT in server-side session, not browser local storage.
- MVC cookies are HttpOnly and use SameSite=Lax. HTTPS should be used in production.
- Every MVC POST form uses anti-forgery validation.
- The API is authoritative for roles and resource ownership; hiding a navigation link is not treated as access control.
- Image uploads accept JPEG, PNG and WebP, with a 5 MB limit.
- The Azure connection string is configured only in the API.
- The current API permits public registration as either role to match the existing Part 2 behaviour. A real production system should add organiser approval or invitation.
- The public event/category browse endpoints expose event information without requiring login; enrolment still requires a participant token.
- The MVC application displays API failures as user-facing messages rather than silently treating failed requests as successful.

## Part 3 manual assessment test plan

1. Register and log in as an organiser.
2. Confirm organiser-only navigation and that participant-only pages reject the organiser role.
3. Create an event with a name, description, date, location, route, distance and type.
4. Upload a JPG/PNG/WebP event banner and verify it displays.
5. Add, edit and delete categories.
6. Register and log in as a participant in another browser/private session.
7. Browse and filter events, open event details and inspect categories.
8. Enrol in a category; verify the enrolment appears in My Events.
9. Cancel an enrolment without a result; verify the status/list updates.
10. As organiser, inspect event enrolments and capture a participant result.
11. As participant, verify My Results shows the event date, category, finish time and position/total finishers.
12. Update profile details and upload a profile picture.
13. Attempt direct URL access to the wrong role's pages and verify access is denied.
14. Log out and verify protected pages redirect to login.
15. Run the database migration, API tests, MVC build and Docker container.

## GitHub branch setup

Continue from the Part 2 branch and create a separate Part 3 branch before committing these changes:

```bash
git switch part-2-rest-api
git switch -c part-3-mvc-webapp
```

If you are applying this ZIP to an existing clone, copy the project files into that checked-out branch rather than replacing the repository's `.git` directory. The ZIP itself does not contain Git history or create/push a remote branch.

## GitHub commit guidance

Use meaningful commits to show the development process. Suggested sequence:

1. Add MVC project and base configuration.
2. Add typed API client.
3. Add session authentication.
4. Add role-aware navigation and route protection.
5. Add participant event browsing.
6. Add event details and category display.
7. Add participant enrolment workflow.
8. Add My Events and cancellation.
9. Add My Results.
10. Add organiser dashboard.
11. Add organiser event CRUD.
12. Add category management.
13. Add organiser enrolment management.
14. Add result capture.
15. Add profile editing.
16. Add Azure Blob Storage service.
17. Add event banner upload.
18. Add profile picture upload.
19. Add MVC Docker configuration.
20. Extend GitHub Actions and documentation.

These are suggested commit boundaries; create commits as you implement/review the work rather than manufacturing history after the fact.

## Demonstration video

Record an unlisted video showing the two roles and the manual assessment test plan above. Add the final unlisted YouTube URL here before submission. Do not submit the placeholder as a real video link.

## AI use disclosure

Update this statement to accurately reflect your own work and institutional requirements. ChatGPT was used to assist with code scaffolding, API/MVC integration planning, documentation and review. The submitted implementation should be personally reviewed, tested, understood and explained by the student. Do not claim code was independently authored if AI-generated assistance was used.
