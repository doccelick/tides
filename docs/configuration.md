# Configuration

All settings below can be committed except `MSSQL_SA_PASSWORD` and connection strings that contain it. The password should be written in an untracked `.env` file in the repo root. `.env.example` contains the relevant variables.

## API

The API reads its settings from `api/src/Tides.Api/appsettings.json`. In the Development environment, `appsettings.Development.json` overrides them. Environment variables override both, with `__` in place of `:`, so `Kartverket:BaseAddress` becomes `Kartverket__BaseAddress`. A list takes one variable per item, numbered from 0, so the first allowed origin is `Cors__AllowedOrigins__0`.

| Setting | Default | What it does |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | The environment the API runs in. `Development` loads `appsettings.Development.json` and turns on the OpenAPI document and Scalar. `launchSettings.json` and `compose.yml` set it to `Development`. Not read from appsettings files. |
| `Kartverket:BaseAddress` | `https://vannstand.kartverket.no/` | Address of Kartverket's Tide API. The API does not start unless it is an absolute http or https address. |
| `Cors:AllowedOrigins` | Empty, and `http://localhost:5173` in Development | Browser origins allowed to call the API's GET endpoints from another origin. Each origin is a scheme, host and port with no trailing slash. When the list is empty, browsers block all cross-origin requests. |
| `ConnectionStrings:Tides` | Not set | SQL Server connection string for `TidesDbContext`. `compose.yml` sets it to the `mssql` service with the password from `MSSQL_SA_PASSWORD`. Outside Compose, `dotnet ef database update` takes it through `--connection`, with `127.0.0.1,1433` as the server. `localhost` can resolve to IPv6 first and time out, since Compose publishes the port on IPv4 only. |

## Frontend

The Vite dev server reads its settings from environment variables.

| Setting | Default | What it does |
| --- | --- | --- |
| `API_URL` | `http://localhost:8080` | Where the dev server proxies `/health` and `/v1`. `compose.yml` sets it to `http://api:8080`, the API's service name inside Compose. |

## Compose

Compose reads these variables from `.env` in the repo root.

| Setting | Default | What it does |
| --- | --- | --- |
| `MSSQL_SA_PASSWORD` | None, required | Password for SQL Server's `sa` login in the `mssql` service, also used in the API's connection string. It must be at least 8 characters and use three of uppercase letters, lowercase letters, digits and symbols, or SQL Server stops during startup. It must not contain `;`, `=` or quotes, which break the connection string. |
