# Development

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) 20.19+ or 22.12+, for the frontend
- [Docker Desktop](https://www.docker.com/products/docker-desktop/), for building and running the images

## Repository layout

| Path | Contents |
| --- | --- |
| `api/Tides.slnx` | Solution for the API and its tests |
| `api/src/Tides.Api` | .NET 10 minimal API |
| `api/tests/Tides.Api.Tests` | xUnit v3 tests, run on Microsoft.Testing.Platform |
| `frontend/` | React frontend: Vite, TypeScript, Tailwind CSS, shadcn/ui |
| `docs/` | Documentation |

## Run the API

From the repository root:

```shell
dotnet run --project api/src/Tides.Api
```

The API listens on `http://localhost:5129` and runs in the Development environment.

| URL | What it is |
| --- | --- |
| `http://localhost:5129/health` | Health check, returns `Healthy` |
| `http://localhost:5129/openapi/v1.json` | OpenAPI document, Development only |
| `http://localhost:5129/scalar/v1` | Scalar UI for browsing and calling the API, Development only |

## Run the tests

```shell
dotnet test api/Tides.slnx
```

`global.json` switches `dotnet test` to Microsoft.Testing.Platform.

## Build and run the container

```shell
docker build -t tides-api api
docker run --rm -p 8080:8080 tides-api
```

The container listens on port 8080 and runs as a non-root user. It runs in the Production environment, so the OpenAPI document and Scalar are off. To turn them on, add `-e ASPNETCORE_ENVIRONMENT=Development` to `docker run`.

## Run the frontend

Start the API first, then from `frontend/`:

```shell
npm install
npm run dev
```

The frontend runs on `http://localhost:5173`. The start page shows whether the API answers on `/health`.

The Vite dev server proxies `/health` to the API, so the browser makes no cross-origin requests. The proxy target is `http://localhost:5129`; set the `API_URL` environment variable to point it elsewhere.

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

## Run the frontend in a container

`frontend/Dockerfile.dev` runs the Vite dev server in a container. With the API running on the host:

```shell
docker build -f frontend/Dockerfile.dev -t tides-frontend-dev frontend
docker run --rm -p 5173:5173 -e API_URL=http://host.docker.internal:5129 tides-frontend-dev
```

`host.docker.internal` lets the container reach the API on the host.
