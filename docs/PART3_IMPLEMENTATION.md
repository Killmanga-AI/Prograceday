# Part 3 implementation notes

## Separation of concerns

`RaceDay.Mvc` is an ASP.NET Core MVC presentation client. It sends HTTP requests to `RaceDay.Api` through `RaceDayApiClient`. It does not reference the EF Core context or SQL Server provider and has no Azure Storage dependency.

The API remains the source of truth for:
- authentication and JWT issuance;
- role and organiser ownership checks;
- event, category, enrolment and result rules;
- participant-specific data;
- database persistence;
- image validation and Azure Blob Storage.

## Session model

After a successful API login, MVC stores the token, user ID, full name, email and role in server-side ASP.NET Core session. API calls attach the bearer token. The MVC `SessionAuthorizeAttribute` filter protects controller actions and checks the role before executing them. The API independently enforces the same role/ownership requirements.

The session cookie is HttpOnly and SameSite=Lax. Use HTTPS in a deployed environment.

## API additions

- Public `GET /api/events` and `GET /api/events/{id}` support event discovery.
- Public `GET /api/events/{id}/categories` allows visitors to inspect event categories before signing in.
- `GET /api/organiser/dashboard` returns organiser-scoped summary values and recent/upcoming event data.
- `GET /api/organiser/events` returns the authenticated organiser's event list.
- Participant enrolment responses include event ID, event date, location, type, banner URL and category distance.
- Participant result responses include event ID/date/location, category distance and total recorded finishers in the same category.
- `POST /api/events/{id}/banner` and `POST /api/users/me/profile-picture` accept multipart form data and store media through the API's Azure Blob service.

## Image upload policy

Only JPEG, PNG and WebP are accepted. The API checks declared content type, verifies file signature bytes, and enforces a 5 MB limit. The organiser must own the event before uploading its banner. The authenticated user can upload only their own profile picture.

The sample implementation creates a blob container with blob-level public read access so images can be displayed by the MVC browser. The Azure Storage account must permit public blob access. For deployments requiring private media, use short-lived SAS URLs and update the service before production.

## Database migration

`20261109190000_AddPart3ImageUrls` adds nullable URL columns:
- `Events.BannerImageUrl`
- `Users.ProfilePictureUrl`

Existing records remain valid because both columns are nullable. The migration is reversible.

## Test scope

API integration tests cover public event/category browsing, organiser dashboard scoping, participant result metadata and media validation/configuration failures. The test host uses EF Core InMemory. A separate SQL Server migration and Azure Blob Storage verification is still required before submission.
