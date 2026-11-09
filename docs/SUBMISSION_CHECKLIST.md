# Submission checklist

## Code and build

- [x] Whole solution builds in Release with 0 warnings and 0 errors.
- [x] API integration tests pass (29/29).
- [x] MVC integration tests pass (40/40).
- [x] MVC has no direct database or Azure dependency.
- [x] Session authentication, roles, expiry and logout implemented.
- [x] Upload type/size validation on MVC and API.
- [x] Friendly status pages and API-unavailable handling.
- [x] Responsive UI.
- [x] Dockerfiles for API and MVC.
- [x] Compose stack with SQL Server, network, volumes and health checks.
- [x] `.env.example` documents required secrets; `.env` is ignored.
- [x] GitHub Actions builds, tests, verifies migrations and builds images.
- [x] README covers setup, configuration, tests, Docker and endpoints.
- [x] Rubric mapping, implementation notes and test results in `docs/`.
- [x] 26 meaningful Part 3 commits on `part-3-mvc-webapp` on top of `part-2-rest-api`.

## Before you submit

- [x] Push `part-3-mvc-webapp` to your GitHub repository and confirm the Actions run is green.
- [x] Built and started the Compose stack locally; all three services healthy and the register → login → create-event → MVC login flow passed.
- [x] Applied migrations to SQL Server 2022 in Compose (automatic startup migration) and confirmed `/health` reports the database connected.
- [ ] Repeat the Compose run on the submission machine and click through both roles in a browser.
- [x] Capture screenshots of the successful Part 3 build/test/migration/Docker run and docs validation (`docs/part3-github-actions-success.png`, `docs/part3-docs-validation-success.png`).
- [ ] If required, configure Azure Blob Storage and upload a banner and profile picture; otherwise state that the local provider was used.
- [ ] Record the unlisted YouTube demo using `docs/DEMO_SCRIPT.md` and replace the README placeholder.
- [ ] Add any institution-required cover page or declaration.
- [ ] Review the AI-use disclosure and make it accurate.
- [x] Create the final ZIP **without** `.git/`, `bin/`, `obj/`, `.env`, user secrets or databases.
- [x] Verify the ZIP contains `README.md`, `docs/`, both Dockerfiles, `docker-compose.yml`, `.env.example` and the solution.

## Suggested ZIP command (from the repository root)

```bash
git archive --format=zip --output=../RaceDay-Part3-Submission.zip HEAD
```

`git archive` includes tracked files only, so build output, `.git`, `.env` and local media are excluded automatically.

## Commit hygiene

- Keep commits focused and messages meaningful.
- Do not rewrite or squash the existing Part 1/2 history.
- Never commit secrets, connection strings, `.env`, `bin/`, `obj/` or uploaded media.
