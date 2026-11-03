# Roadmap

Living document, updated at the end of each development loop.

Status: **Done** · **Next** · **Planned** · **Later**

## API foundation — Done

- [x] Job application model and EF Core mapping (PostgreSQL, snake_case)
- [x] CRUD endpoints with typed results
- [x] Request validation with FluentValidation
- [x] Automatic timestamps (SaveChanges interceptor + `TimeProvider`)
- [x] First migration
- [x] Integration tests (Testcontainers, Respawn)
- [x] OpenAPI document and Swagger UI (Development)
- [x] Developer documentation: README, reference notes, security audit, roadmap

## Endpoint refinement — Next

- [ ] Pagination on `GET /job-applications`, with a server-side maximum page size
- [ ] String enums in the JSON contract, same value in requests and responses
- [ ] Align `AppliedAt` / `AppliedOn` naming
- [ ] Validation aligned with database constraints (max lengths, `http`/`https` URLs only)
- [ ] Longer URL column (2048)
- [ ] Consistent error responses (ProblemDetails, exception handler)
- [ ] Stable validation errors (fixed culture, camelCase keys)
- [ ] CORS policy for the front-end, with explicit origins
- [ ] Bind PostgreSQL to `127.0.0.1` in `docker-compose.yaml`
- [ ] OpenAPI metadata (names, summaries, tags)

## Refactoring — Planned

- [ ] Split `Program.cs`: one endpoint group (`MapGroup`) per feature
- [ ] Move service registration into extension methods

## CI/CD — Planned

- [ ] GitHub Actions: build, format check, tests
- [ ] Vulnerable dependency check and Dependabot (NuGet, Docker, GitHub Actions)
- [ ] Docker image for the API

## Local instance & data-preserving migrations — Planned

- [ ] Squash development migrations before the first release
- [ ] Dedicated local database, separate from the development one
- [ ] Apply migrations with EF Core migration bundles
- [ ] Back up the database (`pg_dump`) before each migration
- [ ] Schema changes that keep existing data

## Public demo — Later

- [ ] Deployment
- [ ] Hardening for public exposure: rate limiting, `AllowedHosts`, forwarded headers, HSTS
- [ ] Demo data: seed and periodic reset

## Not planned for now

- Authentication and multi-user support (local tool by design)
