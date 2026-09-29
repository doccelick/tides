# Development

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/), for building and running the API image

## Repository layout

| Path | Contents |
| --- | --- |
| `api/Tides.slnx` | Solution for the API and its tests |
| `api/src/Tides.Api` | .NET 10 minimal API |
| `api/tests/Tides.Api.Tests` | xUnit v3 tests, run on Microsoft.Testing.Platform |
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
