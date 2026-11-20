# Ambio API

Backend of **Ambio**, a simple job application tracker.

This project is a rewrite of the [Ambio proof of concept](https://github.com/Kevin-HESSE/Ambio), originally built with
Blazor. The API is now a standalone backend.

It is meant to run locally as a personal tool, with a public demo as a showcase. There is no authentication, by design.
The front-end is a separate project (work in progress).

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

- **.NET 10 SDK**: see the official [Microsoft documentation](https://dotnet.microsoft.com/en-us/download/dotnet), or
  use [`dotnetup`](https://learn.microsoft.com/dotnet/core/tools/dotnetup/).
- **Docker**: see the official [Docker documentation](https://docs.docker.com/get-docker/). It is needed for the
  database and for the tests.

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

- The connection string is in `src/Ambio.Api/appsettings.Development.json`. It can be overridden with the
  `ConnectionStrings__DefaultConnection` environment variable.
- The database container reads `POSTGRES_PORT`, `POSTGRES_USER`, `POSTGRES_PASSWORD` and `POSTGRES_DB` from a `.env`
  file (ignored by git). Defaults: port `5432`, `ambio` for the rest. If you change them, update the connection string
  too.

## Running tests

Docker must be running. Testcontainers starts its own PostgreSQL container, so the `docker compose` database is not
needed.

```shell
# All tests
dotnet test

# One test class
dotnet test --project tests/Ambio.Api.IntegrationTests --filter-class "*CreateOfferTests"

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
├── Fixtures/            # Test data builders and fake clock helpers
└── AmbioApiFactory.cs, BaseIntegrationTests.cs
docs/                    # Conception, reference notes, explorations and audits (in French)
```

Project references:

```text
Ambio.Api ──► Ambio.Application ──► Ambio.Data ──► Ambio.Domain
    └───────► Ambio.Migrations ─────────┘
```

- `Ambio.Domain` has no dependencies.
- EF Core is only referenced by `Ambio.Data` and `Ambio.Migrations`. `Ambio.Application` uses the `DbContext` directly,
  without a repository layer.
- `Ambio.Api` is the startup project. It references `Ambio.Migrations` so that the migrations are loaded at runtime.

## Database & migrations

The `DbContext` lives in `Ambio.Data`, and the migrations in `Ambio.Migrations`. Every `dotnet ef` command needs two
options:

- `--project src/Ambio.Migrations`: where the migrations are written;
- `--startup-project src/Ambio.Api`: the project that is run to build the configured `DbContext` (connection string,
  naming convention, interceptors).

New migrations are created at the root of `Ambio.Migrations`, next to the model snapshot, in the `Ambio.Migrations`
namespace.

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

| Method | Route                            | Description                         | Responses                  |
|--------|----------------------------------|-------------------------------------|----------------------------|
| `GET`  | `/job-applications`              | Paginated, filtered and sorted list | `200`, `400`               |
| `GET`  | `/job-applications/{id}`         | Details with status history         | `200`, `404`               |
| `POST` | `/job-applications`              | Create an application for an offer  | `201`, `400`               |
| `POST` | `/job-applications/spontaneous`  | Create a spontaneous application    | `201`, `400`               |
| `PUT`  | `/job-applications/{id}`         | Update title, company and URL       | `200`, `400`, `404`, `409` |
| `POST` | `/job-applications/{id}/status`  | Change the status                   | `204`, `400`, `404`, `409` |
| `POST` | `/job-applications/{id}/archive` | Archive an application              | `204`, `404`, `409`        |
| `POST` | `/job-applications/{id}/reopen`  | Reopen an archived application      | `204`, `404`, `409`        |

Main rules:

- An application is either for an offer (with a `url`) or spontaneous (no `url`).
- A new application starts with the `sent` status. Statuses then follow a workflow: `sent` → `interview` → `offer` →
  `taken`, and `rejected` or `withdrawn` at any step before the end. Going back is not possible.
- Archived applications are read-only: any change returns `409`.
- Enums are strings in requests and responses (`"interview"`), case-insensitive.
- Dates use the `yyyy-MM-dd` format.
- Errors are returned as ProblemDetails.

The full contract (filters, request bodies, responses) is described in French in [
`docs/conception/endpoints.md`](docs/conception/endpoints.md).

Ready-to-use requests are in [`src/Ambio.Api/Ambio.Api.http`](src/Ambio.Api/Ambio.Api.http).

## Documentation

- [ROADMAP.md](ROADMAP.md): what is done and what comes next.
- [docs/conception](docs/conception): functional description, tables and endpoints, in French.
- [docs/references](docs/references): reference notes on the stack, in French.
- [docs/explorations](docs/explorations): technical explorations, in French.
- [docs/audits](docs/audits): security and technical audits, in French.
- [CLAUDE.md](CLAUDE.md): guidelines for Claude Code.
