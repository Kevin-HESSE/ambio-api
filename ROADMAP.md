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

- [x] Pagination on `GET /job-applications`, with a server-side maximum page size
- [x] Filters on `GET /job-applications`: repeatable `status`, `search`, `company`, `archived` (case-insensitive)
- [x] Sorting on `GET /job-applications`: `order[date]`, `order[title]`
- [x] Integration tests for filters, sorting and validation on `GET /job-applications`
- [x] ProblemDetails for empty error responses
- [ ] String enums in the JSON contract, same value in requests and responses (responses done, request bodies
  still accept numbers)
- [ ] Align `AppliedAt` / `AppliedOn` naming (responses done, request still uses `appliedAt`)
- [ ] Validation aligned with database constraints (max lengths, `http`/`https` URLs only)
- [ ] Longer URL column (2048)
- [ ] Exception handler for unhandled errors
- [ ] Stable validation errors (fixed culture, keys named after the request fields; done for collection filters)
- [ ] CORS policy for the front-end, with explicit origins
- [ ] Bind PostgreSQL to `127.0.0.1` in `docker-compose.yaml`
- [ ] OpenAPI metadata (names, summaries, tags)
- [ ] Integration tests for invalid payloads (`400` on create and update)
- [x] Typed result on `GET /job-applications` for the OpenAPI document

## Layered architecture — Done

- [x] Split the solution into projects: `Domain`, `Data`, `Migrations`, `Application`, `Api`
- [x] Migrations in a dedicated project
- [x] Business logic moved from endpoints to application services, with validation in the service
- [x] Split `Program.cs`: one endpoint group (`MapGroup`) per feature
- [x] Move service registration into extension methods
- [x] `CancellationToken` passed from endpoints to EF Core

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
