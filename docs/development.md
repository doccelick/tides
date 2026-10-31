# Development

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/), for running the stack with Docker Compose and for the API integration tests
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), for running and testing the API without Docker
- [Node.js](https://nodejs.org/) 22.22.2+, 24.15+ or 26+, for running and testing the frontend without Docker

The SQL Server image runs only on x86-64. Microsoft doesn't support it under emulation, such as Rosetta 2 on Apple silicon or Prism on Windows on Arm. On those machines, run only the unit tests, as described in [testing.md](testing.md#run-the-tests).

## Repository layout

| Path | Contents |
| --- | --- |
| `compose.yml` | Docker Compose file for running SQL Server, the API and the frontend together |
| `.env.example` | Template for `.env`, which holds the SQL Server password for Compose |
| `dotnet-tools.json` | Local .NET tools, here `dotnet-ef` for EF Core migrations |
| `api/Tides.slnx` | Solution for the API and its tests |
| `api/src/Tides.Api` | .NET 10 minimal API |
| `api/tests/Tides.Api.Tests` | xUnit v3 unit tests, run on Microsoft.Testing.Platform |
| `api/tests/Tides.Api.IntegrationTests` | xUnit v3 integration tests against SQL Server in a Testcontainers container |
| `frontend/` | React frontend: Vite, TypeScript, Tailwind CSS, shadcn/ui |
| `docs/` | Documentation |

## Run with Docker Compose

Compose needs a password for SQL Server. Copy `.env.example` to `.env` in the repository root and set `MSSQL_SA_PASSWORD`, following the rules in [configuration.md](configuration.md#compose).

From the repository root:

```shell
docker compose up --watch
```

This builds the API and frontend images and starts them together with SQL Server. The API runs in the Development environment, so the OpenAPI document and Scalar are on. Before the API starts, the `migrate` service brings the database schema up to date, as described under [Database](#database).

| URL | What it is |
| --- | --- |
| `http://localhost:5173` | Frontend |
| `http://localhost:8080/health` | API health check, returns `Healthy` |
| `http://localhost:8080/v1/stations` | Kartverket's permanent tide stations, cached in memory for 24 hours |
| `http://localhost:8080/openapi/v1.json` | OpenAPI document |
| `http://localhost:8080/scalar/v1` | Scalar UI for browsing and calling the API |

`--watch` keeps the containers in step with your files:

| When you save | What happens |
| --- | --- |
| A file in `frontend/src` | The file is copied into the container and Vite hot-reloads the page |
| `frontend/package.json` or `package-lock.json` | The frontend image is rebuilt and its container replaced |
| A file in `api/src` | The API image is rebuilt and its container replaced |

Inside Compose the frontend reaches the API by its service name, through the `API_URL` setting described in [configuration.md](configuration.md#frontend).

Stop with `Ctrl+C`. `docker compose down` removes the containers and the network.

## Database

SQL Server runs in the `mssql` service and keeps its data in the `mssql-data` volume, so locations and imports survive restarts and `docker compose down`. Compose publishes it on `127.0.0.1:1433` for tools that run on the host.

The API does not create or update the schema itself. Compose does it with the `migrate` service:

1. `mssql` reports healthy once SQL Server accepts logins.
2. `migrate` runs an EF Core migration bundle, built from the `migrate` stage of `api/Dockerfile`, which creates the `Tides` database if needed and applies the missing migrations. It then exits.
3. `api` starts only after `migrate` has exited successfully.

If `migrate` fails, the API does not start. `docker compose logs migrate` shows why.

When the API runs outside Compose, apply the migrations from the repository root instead:

```shell
dotnet tool restore
dotnet ef database update --project api/src/Tides.Api
```

`dotnet ef` runs on the host, so it reads the connection string from user secrets. Set it once as described under `ConnectionStrings:Tides` in [configuration.md](configuration.md#api).

### Add a migration

After changing an entity or `TidesDbContext`, add a migration from the repository root:

```shell
dotnet ef migrations add <Name> --project api/src/Tides.Api --output-dir Data/Migrations
```

The migration files go into `api/src/Tides.Api/Data/Migrations` and are committed with the change that needs them.

`--watch` does not run `migrate` again. To apply a new migration while the stack runs, rebuild and run it from a second terminal:

```shell
docker compose up --build migrate
```

### Reset the database

To start from an empty database, remove the containers together with the `mssql-data` volume and start the stack again. `migrate` creates the database and applies every migration.

```shell
docker compose down -v
docker compose up --watch
```

With the stack running, you can instead drop only the database and run `migrate` again:

```shell
dotnet ef database drop --force --project api/src/Tides.Api
docker compose up migrate
```

## Run the API

The API needs SQL Server and the connection string in user secrets, as described under [Database](#database). To run SQL Server without the other services, start only the `mssql` service, then the API, from the repository root:

```shell
docker compose up -d mssql
dotnet run --project api/src/Tides.Api
```

The API listens on `http://localhost:8080` and runs in the Development environment, with the same URLs as under Compose.

## Run the tests

```shell
dotnet test api/Tides.slnx
```

The integration tests need Docker. [testing.md](testing.md) describes how the tests are organized and how to run only some of them.

## Run the frontend

Start the API first, then from `frontend/`:

```shell
npm install
npm run dev
```

The frontend runs on `http://localhost:5173`. The page shows a map with Kartverket's tide stations as round markers and saved locations as square markers, and a badge that shows whether the API answers on `/health`. Tapping a marker opens a card with the water level and the tides, which you can swipe or drag through. A station shows its latest measured level, and a location shows a level estimated from the nearest station.

The "Sign in" button under the badge signs in with Microsoft Entra ID through a redirect to Microsoft's sign-in page. Only accounts assigned in the Tides SPA app registration can sign in, and the admin tools below appear only for an account with the `Admin` role. Each save or delete sends an access token for the API, and the API checks the role again. When the session has expired, the form shows "Sign in again to save changes".

To add a location as an admin, hold the left mouse button or a finger still on the map for half a second and choose "Add location?". The form takes a name and a range such as "Next 31 days" or "This month", and saves the location with an import of that range. For an admin, a location's card has an "Imports" section that lists its imports, imports more ranges and deletes an import, and a button that deletes the location. Each tide tile also shows whether it came from the database or from Kartverket.

The Vite dev server proxies `/health` and `/v1` to the API, so the browser makes no cross-origin requests. The proxy target is `http://localhost:8080`, and `API_URL` points it elsewhere, as described in [configuration.md](configuration.md#frontend).

## Frontend scripts

Run from `frontend/`:

| Command | What it does |
| --- | --- |
| `npm run test` | Runs the Vitest tests once |
| `npm run test:watch` | Runs the tests in watch mode |
| `npm run lint` | Lints with oxlint |
| `npm run fmt` | Formats with oxfmt |
| `npm run fmt:check` | Checks formatting without changing files |
| `npm run build` | Type-checks and builds to `dist/` |
| `npm run gen:api` | Generates TypeScript types for the API from `openapi.json` into `src/api/generated/` |

## Build the images

Compose builds all three images. To build one on its own:

```shell
docker build -t tides-api api
docker build --target migrate -t tides-migrate api
docker build -f frontend/Dockerfile.dev -t tides-frontend-dev frontend
```

The `migrate` image holds only the migration bundle. It reads the connection string from `ConnectionStrings__Tides` like the API, or from `--connection`.

The API image listens on port 8080 and runs as a non-root user. It defaults to the Production environment, where the OpenAPI document and Scalar are off. Compose sets `ASPNETCORE_ENVIRONMENT=Development` to turn them on.

`frontend/Dockerfile.dev` runs the Vite dev server and is for development only.

## OpenAPI document

`dotnet build` regenerates `api/src/Tides.Api/openapi.json` from the endpoints. The file is committed, so an endpoint change and its updated `openapi.json` go in the same commit.

`npm run gen:api` from `frontend/` generates the frontend's TypeScript types from `openapi.json` into `frontend/src/api/generated/`, using @hey-api/openapi-ts. The generated files are committed as well, so an endpoint change carries its regenerated types in the same commit.

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request to `main`. Its two jobs must pass before the pull request can merge.

| Job | Checks |
| --- | --- |
| `api` | `dotnet format` verification, build, tests, and that `openapi.json` matches the build output |
| `frontend` | oxlint, oxfmt check, type-check and build, Vitest tests, and that the generated API types match `openapi.json` |

To fix a formatting failure, run `dotnet format api/Tides.slnx` from the repository root or `npm run fmt` from `frontend/`, then commit the result.

Two more checks run on pull requests from branches in this repository:

- CodeQL scans the C#, TypeScript and workflow code for security issues. It uses GitHub's default setup, turned on in the repository's code security settings, so it has no workflow file.
- CodeRabbit reviews the changes once the pull request is ready for review and comments on it. `.coderabbit.yaml` excludes generated files from its review.

## Dependency updates

Dependabot checks NuGet, npm and GitHub Actions dependencies weekly, as configured in `.github/dependabot.yml`. Minor and patch updates arrive as one pull request per ecosystem. Major updates get their own pull request, except for GitHub Actions, which are grouped together. Major TypeScript updates are ignored, for the reasons in [ADR 0003](adr/0003-typescript-6-over-typescript-7.md).
