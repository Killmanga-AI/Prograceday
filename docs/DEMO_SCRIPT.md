# Demonstration video script

Target length: 10–12 minutes. Record at 1080p, narrate each step, and keep the terminal and browser readable. Use two browser profiles (or one normal and one private window) so the organiser and participant sessions do not overwrite each other.

## 1. Introduction (0:00–0:45)

- State the project: RaceDay, PROG6212 Part 3.
- Show the repository branch `part-3-mvc-webapp` and `git log --oneline`.
- Explain the architecture: MVC → REST API → SQL Server; MVC holds the JWT in server-side session; uploads go through the API to Azure Blob Storage or the local provider.

## 2. Build and tests (0:45–2:00)

```bash
dotnet build RaceDay.sln -c Release
dotnet test RaceDay.Tests/RaceDay.Tests.csproj -c Release --no-build
dotnet test RaceDay.Mvc.Tests/RaceDay.Mvc.Tests.csproj -c Release --no-build
```

- Point out 0 warnings and 69 passing tests.
- Briefly show one API test and one MVC test file to explain the stub API approach.

## 3. Start the system (2:00–3:00)

- Show the API terminal: migrations/seed messages and `Now listening on`.
- Open `http://localhost:5171/health` and Swagger.
- Start MVC and open the home page.

## 4. Organiser journey (3:00–6:30)

1. Register an organiser and show the role-aware navigation.
2. Create an event with all required fields and upload a banner.
3. Show validation: submit an empty form, then an oversized/PDF banner.
4. Add two categories and edit one.
5. Open the dashboard and note the totals.
6. Open enrolments (empty for now).
7. Attempt to open a participant-only URL and show access denied.

## 5. Participant journey (6:30–9:30)

1. In the second profile, register a participant.
2. Browse and filter events; open the organiser's event.
3. Enrol in a category; show the remaining-places update.
4. Open My Events and cancel one enrolment.
5. Switch back to the organiser, capture a finish time and position for the participant.
6. Switch back to the participant and open My Results: show event date, category, finish time and position out of finishers.
7. Update profile details and upload a profile picture.

## 6. Security and resilience (9:30–10:45)

- Show logout, then direct access to a protected page redirecting to login.
- Show that a POST without an antiforgery token is rejected (optional: DevTools or a test).
- Stop the API and reload an MVC page to show the friendly unavailable page.
- Restart the API.

## 7. Docker and CI (10:45–11:45)

- Show `docker compose config` and, if Docker is running, `docker compose up --build`.
- Open MVC on `http://localhost:8080` and Swagger on `http://localhost:8081/swagger`.
- Show the GitHub Actions run: build/test, SQL migration check and Docker image builds.

## 8. Wrap-up (11:45–12:00)

- Summarise what was built, the test evidence, and one known limitation (Azure credentials/local media provider).
- State the unlisted video URL in the README.

## Recording checklist

- [ ] No secrets, connection strings or `.env` contents on screen.
- [ ] Clear narration of the MVC/API boundary.
- [ ] Both roles demonstrated.
- [ ] At least one validation failure and one access-denied case shown.
- [ ] Test output visible long enough to read.
- [ ] Video uploaded as unlisted; link added to `README.md`.
