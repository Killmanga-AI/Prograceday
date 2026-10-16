# RaceDay Part 2 traceability matrix

| Assessment item | Files | Validation |
|---|---|---|
| ASP.NET Core Web API and EF Core setup | `RaceDay.Api/Program.cs`, `RaceDay.Api/RaceDay.Api.csproj`, `RaceDay.Api/Migrations/` | `dotnet build` and `dotnet test` |
| Role and user model | `RaceDay.Api/Models/Entities.cs`, `RaceDay.Api/Data/RaceDayContext.cs` | Auth and profile tests |
| JWT security | `RaceDay.Api/Services/TokenService.cs`, `RaceDay.Api/Program.cs` | Login token assertions |
| Registration and login | `RaceDay.Api/Controllers/AuthController.cs` | Register/login tests |
| User profile management | `RaceDay.Api/Controllers/UsersController.cs` | Auth flow and profile tests |
| Event CRUD and ownership | `RaceDay.Api/Controllers/EventsController.cs` | Event creation and forbidden access tests |
| Categories management | `RaceDay.Api/Controllers/CategoriesController.cs` | Category ownership checks |
| Enrolments and results | `RaceDay.Api/Controllers/EnrolmentsController.cs` | Result and registration sets |
| Swagger and API docs | `RaceDay.Api/Program.cs` | `/swagger` smoke test |
| CI validation | `.github/workflows/dotnet.yml` | `dotnet test` in GitHub Actions |
