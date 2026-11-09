# Part 3 rubric mapping checklist

This checklist maps the requested Part 3 capabilities to the implementation. Tick each item only after running the application and confirming the behaviour in your own environment.

| Assessment area | Implementation location | Verify |
|---|---|---|
| ASP.NET Core MVC frontend | `RaceDay.Mvc` | [ ] |
| MVC communicates with Part 2 API over HTTP | `Services/RaceDayApiClient.cs` | [ ] |
| Registration and login | `AccountController`, `Views/Account` | [ ] |
| User ID, role and JWT retained in session | `AccountController`, `Program.cs` | [ ] |
| Role-specific navigation | `Views/Shared/_Layout.cshtml` | [ ] |
| Protected pages and logout | `SessionAuthorizeFilter`, `AccountController` | [ ] |
| Organiser dashboard | `OrganiserController.Dashboard`, API dashboard endpoint | [ ] |
| Organiser event create/read/update/delete | `OrganiserController`, `EventsController` API | [ ] |
| Required event fields | `EventViewModel`, `EventRequest` | [ ] |
| Event banner upload | `MediaController`, Azure Blob service | [ ] |
| Category create/update/delete | `OrganiserController`, `CategoriesController` API | [ ] |
| View event enrolments | `OrganiserController.Enrolments` | [ ] |
| Capture results | `OrganiserController.AddResult` | [ ] |
| Browse/filter events | `EventsController.Browse` | [ ] |
| Event detail and categories | `EventsController.Details` | [ ] |
| Participant enrolment | `EventsController.Enrol` | [ ] |
| My Events and status | `EventsController.MyEvents` | [ ] |
| My Results with date and position/finishers | `EventsController.MyResults` | [ ] |
| Profile update | `ProfileController` | [ ] |
| Profile picture upload | `MediaController` | [ ] |
| Azure Blob Storage integration | `RaceDay.Api/Services/BlobStorageService.cs` | [ ] |
| Docker MVC container | root `Dockerfile` | [ ] |
| MVC CI build and API tests | `.github/workflows/dotnet.yml` | [ ] |
| Responsive sporting interface | `RaceDay.Mvc/Views`, `wwwroot/css/site.css` | [ ] |
| Setup and run documentation | root `README.md` | [ ] |
| Unlisted video demonstration | Add URL to README | [ ] |

## Before submission

- [ ] Build the entire solution in Visual Studio or with `dotnet build RaceDay.sln`.
- [ ] Apply EF Core migrations to a backed-up SQL Server database.
- [ ] Run `dotnet test RaceDay.Tests/RaceDay.Tests.csproj`.
- [ ] Verify Azure Blob uploads using your own storage account.
- [ ] Test both roles in separate browser sessions.
- [ ] Test access denial by manually entering the other role's URLs.
- [ ] Build and run the Docker image.
- [ ] Push the work to your Part 3 GitHub branch.
- [ ] Make at least 20 meaningful commits across the implementation.
- [ ] Record and link the unlisted video.
- [ ] Ensure you can explain the API/MVC separation, session/JWT flow, EF migration and Azure upload path.
