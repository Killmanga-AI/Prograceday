# Part 3 implementation notes

## Separation of concerns

`RaceDay.Mvc` is an ASP.NET Core MVC presentation client. It sends HTTP requests to `RaceDay.Api` through `RaceDayApiClient` and has no project reference to the API, no EF Core packages and no Azure Storage dependency.

The API remains authoritative for:

- authentication and JWT issuance;
- role and organiser-ownership checks;
- event, category, enrolment and result rules;
- participant-specific data;
- database persistence and migrations;
- image validation and media storage.

## Request and session flow

1. The visitor submits the MVC login form with an antiforgery token.
2. `AccountController` calls `POST /api/auth/login` through the typed client.
3. On success the MVC app stores the JWT, user ID, name, email and role in server-side session.
4. `RaceDayApiClient` attaches the bearer token to every subsequent API call.
5. `SessionAuthorize` filters check authentication and role before a controller action runs.
6. `SessionExpiryFilter` handles an API `401` by clearing the session and redirecting to login with a message.
7. Logout clears the session; protected pages redirect to login again.

Session cookies are HttpOnly and SameSite=Lax. Secure cookies are on by default outside Development; the Testing environment and local Compose override this because they run over plain HTTP. The API independently enforces the same role and ownership rules, so hiding navigation is never the only control.

## Typed API contracts

The MVC client uses records in `Models/ApiContracts.cs` (`ApiEvent`, `ApiCategory`, `ApiEnrolment`, `ApiResult`, `ApiUser`, dashboard types) instead of untyped `JsonElement` access. Malformed or unexpected API payloads surface as handled errors rather than null-reference exceptions. View models in `Models/ViewModels.cs` carry validation attributes for forms.

## API additions in Part 3

- Public `GET /api/events`, `GET /api/events/{id}` and `GET /api/events/{id}/categories` for discovery.
- `GET /api/organiser/dashboard` for organiser-scoped totals and recent/upcoming events.
- `GET /api/organiser/events` for the authenticated organiser's events.
- Enrolment and result responses include event date, location, type, banner URL, category distance and total finishers.
- `POST /api/events/{id}/banner` and `POST /api/users/me/profile-picture` accept multipart uploads.
- `GET /health` reports API, database and media-provider readiness.

## Media storage

`IBlobStorageService` has two implementations:

- `AzureBlobStorageService` — used when `Media:Provider=Azure`; creates the container with blob-level public read access.
- `LocalBlobStorageService` — the default; writes to `Media:LocalPath` (default `App_Data/media`), serves files from `/media`, and returns an absolute URL using `Media:PublicBaseUrl` or the current request.

The provider is resolved per request, so environment variables, user secrets and test configuration all apply. Compose sets `Media:LocalPath=/app/App_Data/media`, mounts a named volume and sets `Media:PublicBaseUrl` to the host-published API origin so browsers can load images.

Upload rules are enforced in the API controller and service: JPEG/PNG/WebP only, signature bytes checked, 5 MB maximum, and ownership verified before saving.

## Database

`20261109190000_AddPart3ImageUrls` adds nullable `Events.BannerImageUrl` and `Users.ProfilePictureUrl`. Existing rows stay valid and the migration is reversible.

The API seeds the `Organiser` and `Participant` roles at startup. `Database:AutoMigrate` enables migration with bounded retries (useful in Compose where SQL Server starts slowly); it is disabled by default for local runs so migrations stay explicit.

## Errors and resilience

- MVC uses status-code re-execution (`/Home/StatusPage`) for 403/404/500 and a friendly unavailable page when the API cannot be reached.
- The no-cache middleware writes headers **before** the response starts, avoiding "headers already sent" failures.
- The API client maps timeouts, connection failures and non-success responses to user-facing messages.
- Upload validation runs in MVC before the API call, and again in the API.

## Test scope

- `RaceDay.Tests` (29 tests): API integration tests with EF Core InMemory and a temporary local media root.
- `RaceDay.Mvc.Tests` (40 tests): MVC integration tests against a real Kestrel stub API on a loopback port, covering cookies, antiforgery, redirects, multipart uploads and rendered HTML.

SQL Server migrations are verified in CI against a real SQL Server 2022 container. Azure Blob Storage is not covered by automated tests and requires the student's own storage account.
