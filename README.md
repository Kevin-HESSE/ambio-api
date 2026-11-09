# Ambio API

Backend of **Ambio**, a simple job application tracker.

This project is a rewrite of the [Ambio proof of concept](https://github.com/Kevin-HESSE/Ambio), originally built with Blazor. The API is now a standalone backend.

It is meant to run locally as a personal tool, with a public demo as a showcase. There is no authentication, by design. The front-end is a separate project (work in progress).

## Tech stack

| Area       | Technology                                                             |
|------------|------------------------------------------------------------------------|
| Runtime    | .NET 10 (LTS), ASP.NET Core Minimal APIs                               |
| Data       | EF Core 10, Npgsql, PostgreSQL 17 (Docker), snake_case naming          |
| Validation | FluentValidation                                                       |
| API docs   | OpenAPI (`Microsoft.AspNetCore.OpenApi`), Swagger UI                   |
| Tests      | xUnit v3 on Microsoft Testing Platform, Testcontainers, Respawn, Bogus |
| Tooling    | Central Package Management, `dotnet-ef` local tool, `.editorconfig`    |

## Prerequisites

- **.NET 10 SDK**: see the official [Microsoft documentation](https://dotnet.microsoft.com/en-us/download/dotnet), or use [`dotnetup`](https://learn.microsoft.com/dotnet/core/tools/dotnetup/).
- **Docker**: see the official [Docker documentation](https://docs.docker.com/get-docker/). It is needed for the database and for the tests.

## Setup

1. Restore the local tools (`dotnet-ef`):

   ```shell
   dotnet tool restore
   ```

2. Start the database and wait until it is ready:

   ```shell
   docker compose up -d --wait database
   ```

3. Apply the migrations:

   ```shell
   dotnet ef database update --project src/Ambio.Migrations --startup-project src/Ambio.Api
   ```

4. Start the API:

   ```shell
   dotnet run --project src/Ambio.Api
   ```

The API listens on `http://localhost:5262`. In Development:

- Swagger UI: `http://localhost:5262/swagger`
- OpenAPI document: `http://localhost:5262/openapi/v1.json`

### Configuration

- The connection string is in `src/Ambio.Api/appsettings.Development.json`. It can be overridden with the `ConnectionStrings__DefaultConnection` environment variable.
- The database container reads `POSTGRES_PORT`, `POSTGRES_USER`, `POSTGRES_PASSWORD` and `POSTGRES_DB` from a `.env` file (ignored by git). Defaults: port `5432`, `ambio` for the rest. If you change them, update the connection string too.

## Running tests

Docker must be running. Testcontainers starts its own PostgreSQL container, so the `docker compose` database is not needed.

```shell
# All tests
dotnet test

# One test class
dotnet test --project tests/Ambio.Api.IntegrationTests --filter-class "*CreateTests"

# One test method
dotnet test --project tests/Ambio.Api.IntegrationTests --filter-method "*GetById_WhenJobApplicationExists_ShouldReturnOk"
```

## Project structure

The solution is split by layer. Inside each project, files are grouped by feature (`JobApplications/`).

```text
src/
├── Ambio.Domain/        # Entities, enums, abstractions (no dependencies)
├── Ambio.Data/          # EF Core: DbContext, entity mappings, interceptors
├── Ambio.Migrations/    # EF Core migrations and model snapshot
├── Ambio.Application/   # Services, request / response contracts, validators
└── Ambio.Api/           # Endpoints, OpenAPI, service registration (Program.cs)
    └── Ambio.Api.http   # Ready-to-use HTTP requests
tests/Ambio.Api.IntegrationTests/
├── JobApplications/     # One test class per endpoint
└── AmbioApiFactory.cs, BaseIntegrationTests.cs
docs/                    # Reference notes and audits (in French)
```

Project references:

```text
Ambio.Api ──► Ambio.Application ──► Ambio.Data ──► Ambio.Domain
    └───────► Ambio.Migrations ─────────┘
```

- `Ambio.Domain` has no dependencies.
- EF Core is only referenced by `Ambio.Data` and `Ambio.Migrations`. `Ambio.Application` uses the `DbContext` directly, without a repository layer.
- `Ambio.Api` is the startup project. It references `Ambio.Migrations` so that the migrations are loaded at runtime.

## Database & migrations

The `DbContext` lives in `Ambio.Data`, and the migrations in `Ambio.Migrations`. Every `dotnet ef` command needs two options:

- `--project src/Ambio.Migrations`: where the migrations are written;
- `--startup-project src/Ambio.Api`: the project that is run to build the configured `DbContext` (connection string, naming convention, interceptors).

New migrations are created at the root of `Ambio.Migrations`, next to the model snapshot, in the `Ambio.Migrations` namespace.

```shell
# Check whether the model has changes without a migration
dotnet ef migrations has-pending-model-changes --project src/Ambio.Migrations --startup-project src/Ambio.Api

# Create a migration
dotnet ef migrations add <MigrationName> --project src/Ambio.Migrations --startup-project src/Ambio.Api

# Apply migrations
dotnet ef database update --project src/Ambio.Migrations --startup-project src/Ambio.Api

# Remove the last migration (if not applied)
dotnet ef migrations remove --project src/Ambio.Migrations --startup-project src/Ambio.Api
```

To reset the local database, delete the container and its volume:

```shell
docker compose down -v
```

## API

| Method   | Route                     | Responses                  |
|----------|---------------------------|----------------------------|
| `GET`    | `/job-applications`       | `200`                      |
| `GET`    | `/job-applications/{id}`  | `200`, `404`               |
| `POST`   | `/job-applications`       | `201`, `400`               |
| `PUT`    | `/job-applications/{id}`  | `200`, `400`, `404`        |
| `DELETE` | `/job-applications/{id}`  | `204`, `404`               |

Request body for `POST` and `PUT`:

```json
{
  "title": "Backend developer",
  "company": "Acme",
  "url": "https://acme.io/jobs/1",
  "status": 1,
  "appliedAt": "2026-11-03"
}
```

- `status` is sent as a number: `0` Draft, `1` Applied, `2` Interview, `3` Offer, `4` Rejected, `5` Withdrawn. Responses return a display label (`"Offer received"`). Aligning both directions is on the [roadmap](ROADMAP.md).
- `appliedAt` is optional, formatted as `yyyy-MM-dd`.

Ready-to-use requests are in [`src/Ambio.Api/Ambio.Api.http`](src/Ambio.Api/Ambio.Api.http).

## Documentation

- [ROADMAP.md](ROADMAP.md): what is done and what comes next.
- [docs/references](docs/references): reference notes on the stack, in French.
- [docs/audits](docs/audits): security and technical audits, in French.
- [CLAUDE.md](CLAUDE.md): guidelines for Claude Code.
