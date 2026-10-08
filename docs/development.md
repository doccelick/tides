# Development

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/), for running the stack with Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), for running and testing the API without Docker
- [Node.js](https://nodejs.org/) 22.22.2+, 24.15+ or 26+, for running and testing the frontend without Docker

## Repository layout

| Path | Contents |
| --- | --- |
| `compose.yml` | Docker Compose file for running the API and the frontend together |
| `api/Tides.slnx` | Solution for the API and its tests |
| `api/src/Tides.Api` | .NET 10 minimal API |
| `api/tests/Tides.Api.Tests` | xUnit v3 tests, run on Microsoft.Testing.Platform |
| `frontend/` | React frontend: Vite, TypeScript, Tailwind CSS, shadcn/ui |
| `docs/` | Documentation |

## Run with Docker Compose

From the repository root:

```shell
docker compose up --watch
```

This builds both images and starts the API and the frontend. The API runs in the Development environment, so the OpenAPI document and Scalar are on.

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

Inside Compose the frontend reaches the API by its service name, so `API_URL` is set to `http://api:8080`.

Stop with `Ctrl+C`. `docker compose down` removes the containers and the network.

## Run the API

From the repository root:

```shell
dotnet run --project api/src/Tides.Api
```

The API listens on `http://localhost:8080` and runs in the Development environment, with the same URLs as under Compose.

## Run the tests

```shell
dotnet test api/Tides.slnx
```

`global.json` switches `dotnet test` to Microsoft.Testing.Platform.

## Run the frontend

Start the API first, then from `frontend/`:

```shell
npm install
npm run dev
```

The frontend runs on `http://localhost:5173`. The page shows a map with Kartverket's tide stations as markers, and a badge that shows whether the API answers on `/health`.

The Vite dev server proxies `/health` and `/v1` to the API, so the browser makes no cross-origin requests. The proxy target is `http://localhost:8080`. Set the `API_URL` environment variable to point it elsewhere.

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

Compose builds both images. To build one on its own:

```shell
docker build -t tides-api api
docker build -f frontend/Dockerfile.dev -t tides-frontend-dev frontend
```

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
