# RaceDay final audit summary

## Validation commands run

```bash
dotnet test RaceDay.Tests/RaceDay.Tests.csproj --nologo
```

## Observed result

- 6 passing tests
- 0 failed tests
- 0 skipped tests

## Notes

- The API uses SQL Server by default.
- When the environment is set to `Testing`, the API falls back to an in-memory database to keep automated integration tests deterministic.
- The application seeds the `Organiser` and `Participant` role records automatically on startup.
