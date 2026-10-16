# RaceDay – PROG6212 Portfolio of Evidence (Part 1)

## What is RaceDay?

RaceDay is a web-based event management platform built for the South African road running, walking, and cycling community. The idea is pretty straightforward — right now a lot of events are still managed through spreadsheets and WhatsApp groups, which is a mess for both organisers and participants. RaceDay fixes that by giving organisers a proper system to create events and capture results, while participants can browse events, sign up, and track how they're doing over time.

This repo covers Part 1 of the POE, which focuses on system planning and the database design. No API code yet — that comes in Part 2.

---

## The Two Roles

The system is built around two types of users:

- Organiser — someone who runs events. They can create and manage events, add race categories (like a 5km or 10km option), view who's signed up, and capture finish times and positions after the race.
- Participant — someone who enters events. They can browse what's on, enrol in a category, cancel their entry, and check their own results and history.

Role-based access will be enforced at the API level in Part 2.

---

## Entity Relationship Diagram

![RaceDay ERD](docs/ERD.png)

The database has 6 tables: `Roles`, `Users`, `Events`, `Categories`, `Enrolments`, and `Results`. Users play different roles depending on context — the same `Users` table covers both Organisers and Participants, distinguished by a `RoleId` foreign key. The `Enrolments` table acts as a junction between `Users` (as Participants) and `Categories`, and `Results` is linked one-to-one with `Enrolments` since each race entry can only have one captured result.

---

## Setting Up the Database

To get the database running locally:

1. Open SQL Server Management Studio (SSMS) and connect to your local SQL Server instance.
2. Open the file `docs/database_schema.sql`.
3. Run the entire script — it will create a new database called `RaceDayDB`, drop and recreate all 6 tables cleanly, and insert seed data.
4. You can uncomment the `SELECT` statements at the bottom to verify the data loaded correctly.

> Note: The script targets SQL Server. It won't work as-is on MySQL or PostgreSQL.

---

## Docs Folder

All planning documents are in the `/docs` folder:

| File | Description |
|---|---|
| `ERD.png` | Entity Relationship Diagram showing all 6 tables |
| `API_Endpoint_Plan.md` | Full list of 20 planned API endpoints across all functional areas |
| `database_schema.sql` | SQL Server script with schema + seed data |

---

## CI/CD

There's a GitHub Actions workflow at `.github/workflows/validate-docs.yml` that automatically checks the repo structure on every push. It confirms that the `/docs` folder exists and contains all three required files.

**Latest build:**

![CI Build Status](docs/ci-cd%20screenshot.png)

---

## Presentation Video

[![YouTube Video](https://img.shields.io/badge/YouTube-%23FF0000.svg?style=for-the-badge&logo=YouTube&logoColor=white)](YOUR_UNLISTED_YOUTUBE_LINK_HERE)

---

## AI Disclosure

GitHub Copilot was used for autocompletion assistance while writing the SQL script and markdown files. All design decisions — including the ERD structure, table relationships, endpoint planning, and seed data — were made by me. The AI suggestions were reviewed, adjusted, and accepted or rejected based on my own understanding of the system requirements.

---

# RaceDay – Part 2 REST API

ASP.NET Core 8 Web API for the RaceDay event platform. This implementation follows the Part 1 plan and six-table schema: `Roles`, `Users`, `Events`, `Categories`, `Enrolments`, and `Results`.

## Prerequisites

- .NET 8 SDK
- SQL Server / SQL Server LocalDB
- Optional: Visual Studio 2022

## Configure and run

1. Copy `RaceDay.Api/appsettings.Development.json.example` to `RaceDay.Api/appsettings.Development.json`.
2. Update `ConnectionStrings:RaceDay` and the JWT secret.
3. Install EF Core tooling if needed: `dotnet tool install --global dotnet-ef --version 8.0.11`.
4. Create and apply the initial migration:
   ```bash
   dotnet ef migrations add InitialCreate --project RaceDay.Api/RaceDay.Api.csproj --startup-project RaceDay.Api/RaceDay.Api.csproj
   dotnet ef database update --project RaceDay.Api/RaceDay.Api.csproj --startup-project RaceDay.Api/RaceDay.Api.csproj
   ```
5. Start the API: `dotnet run --project RaceDay.Api/RaceDay.Api.csproj`.
6. Open the Swagger UI at `/swagger`.

The development JWT fallback is only for local evaluation. Do not deploy it. Swagger UI is enabled in Development; set `ASPNETCORE_ENVIRONMENT=Development` while recording the assessment demonstration.

## Endpoints

| Method | Endpoint | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| GET / PUT | `/api/users/me` | Authenticated user; own profile only |
| GET | `/api/events`, `/api/events/{id}` | Authenticated user |
| POST | `/api/events` | Organiser |
| PUT / DELETE | `/api/events/{id}` | Owning Organiser |
| GET | `/api/events/{id}/categories` | Authenticated user |
| POST | `/api/events/{eventId}/categories` | Owning Organiser |
| PUT / DELETE | `/api/categories/{id}` | Owning Organiser |
| POST | `/api/categories/{id}/enrolments` | Participant |
| GET | `/api/users/me/enrolments` | Participant; own enrolments |
| GET | `/api/events/{id}/enrolments` | Owning Organiser |
| DELETE | `/api/enrolments/{id}` | Participant; own enrolment |
| POST | `/api/enrolments/{id}/results` | Organiser who owns event |
| GET | `/api/users/me/results` | Participant; own results |
| GET | `/api/events/{id}/results` | Authenticated user |

## Authentication in Swagger

1. Register with a role of `Organiser` or `Participant`.
2. Log in using the same email/password.
3. Copy the returned `token` value.
4. Click **Authorize** in Swagger and paste the token (the Swagger security scheme is HTTP Bearer).
5. Try the endpoints for the role you registered. A request with the wrong role should return `403 Forbidden`; a request without a token should return `401 Unauthorized`.

Passwords are stored using ASP.NET Core's `PasswordHasher<TUser>`, not plaintext. JWTs include the user's ID and role, expire after two hours, and are signed with the configured key. Registration accepts only the two allowed role names. Event/category/result operations check ownership on the server, not only in the UI.

## Tests and CI

Run locally:

```bash
dotnet test RaceDay.Tests/RaceDay.Tests.csproj --nologo
```

A helper script is also included at `scripts/run-local.ps1` for restore, build, and test.

GitHub Actions restores, builds, and runs tests on every push and pull request. The project uses .NET 8 and a test-friendly in-memory database configuration when running under the `Testing` environment.

## Demo plan

1. Show the project structure in the solution explorer.
2. Register a new organiser and participant.
3. Log in and capture the JWT returned by the API.
4. Demonstrate protected routes and role restrictions.
5. Create an event as an organiser and add a category.
6. Enrol a participant and show duplicate or full-category handling.
7. Record a result and confirm the leaderboard output.
8. Show the tests passing from the command line.

## Submission checklist

- [x] Preserve Part 1 ERD and SQL documentation
- [x] Keep the existing docs validation workflow
- [x] Build the .NET 8 API and tests
- [x] Add authentication and authorization
- [x] Add event, category, enrolment, and result endpoints
- [x] Add tests covering auth boundaries and smoke checks
- [ ] Add the final public GitHub Actions build screenshot when available
- [ ] Add the final unlisted YouTube URL when supplied

## Assessment note

This repository preserves the original Part 1 design documents and adds the working ASP.NET Core Part 2 implementation with EF Core migrations, JWT auth, Swagger, and automated testing.
