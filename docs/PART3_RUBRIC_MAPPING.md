# Part 3 rubric mapping

Status reflects the submitted branch `part-3-mvc-webapp`:

- **Done** — implemented and covered by automated tests or verified locally.
- **Manual** — implemented but requires the assessor/student to run it with their own environment.
- **Open** — still needs action before submission.

| Assessment area | Implementation | Status |
|---|---|---|
| ASP.NET Core MVC frontend | `RaceDay.Mvc` | Done |
| MVC consumes the Part 2 API over HTTP only | `Services/RaceDayApiClient.cs`; no API/EF/Azure references in the MVC project | Done |
| Registration and login | `AccountController`, `Views/Account` | Done |
| User ID, role and JWT kept in session | `AccountController`, `Services/SessionAuth.cs`, `Program.cs` | Done |
| Role-specific navigation | `Views/Shared/_Layout.cshtml` | Done |
| Protected pages, expiry and logout | `Filters/SessionAuthorizeFilter.cs`, `Filters/SessionExpiryFilter.cs` | Done |
| Organiser dashboard | `OrganiserController.Dashboard`, `/api/organiser/dashboard` | Done |
| Event create/read/update/delete | `OrganiserController`, API `EventsController` | Done |
| Required event fields and validation | `EventViewModel`, `EventRequest` | Done |
| Event banner upload | `OrganiserController`, API `MediaController` | Done |
| Category create/update/delete | `OrganiserController`, API `CategoriesController` | Done |
| View event enrolments | `OrganiserController.Enrolments` | Done |
| Capture results | `OrganiserController.AddResult` | Done |
| Browse and filter events | `EventsController.Browse` | Done |
| Event details and categories | `EventsController.Details` | Done |
| Participant enrolment and cancellation | `EventsController.Enrol/CancelEnrolment` | Done |
| My Events with status | `EventsController.MyEvents` | Done |
| My Results with date, time and position/finishers | `EventsController.MyResults` | Done |
| Profile update | `ProfileController` | Done |
| Profile picture upload | `ProfileController`, API media endpoint | Done |
| Azure Blob Storage integration | `RaceDay.Api/Services/AzureBlobStorageService.cs`; selected with `Media:Provider=Azure` | Manual — needs Azure credentials |
| Local media fallback for development/tests | `Services/LocalBlobStorageService.cs` | Done |
| MVC and API Dockerfiles | `RaceDay.Mvc/Dockerfile`, `RaceDay.Api/Dockerfile` | Done (built in CI) |
| Compose stack with SQL Server, networking, volumes, health checks | `docker-compose.yml`, `.env.example` | Done (`docker compose config` validated; not run locally) |
| Database migration | `20261109190000_AddPart3ImageUrls`, `dotnet ef` | Done (CI SQL Server job) |
| Automated API tests | `RaceDay.Tests` — 29 passing | Done |
| Automated MVC tests | `RaceDay.Mvc.Tests` — 40 passing | Done |
| GitHub Actions CI | `.github/workflows/dotnet.yml` | Done (runs on push) |
| Responsive sporting interface | Views, `wwwroot/css/site.css` | Done |
| Security controls | Antiforgery, session expiry, role filters, upload validation, headers | Done |
| Setup and run documentation | `README.md`, `docs/` | Done |
| At least 20 meaningful commits | `part-3-mvc-webapp` history | Done |
| Unlisted demonstration video | README placeholder | **Open** |

## How to verify manually

1. Start SQL Server, apply migrations and run the API and MVC app (`README.md`).
2. Register an organiser, create an event with a banner, and add categories.
3. In a private window, register a participant, enrol, and view My Events.
4. As the organiser, view enrolments and capture a result.
5. As the participant, confirm My Results shows date, category, finish time and position.
6. Try the other role's URLs directly and confirm access is denied by the MVC filter and by the API.
7. Upload a PDF or a file over 5 MB and confirm it is rejected before reaching the API.
8. Stop the API and confirm the friendly "service unavailable" page appears.
9. Run `docker compose config`, then `docker compose up --build` on a machine with Docker running.
10. Record the walkthrough in `docs/DEMO_SCRIPT.md` and add the unlisted YouTube link to the README.
