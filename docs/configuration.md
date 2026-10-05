# Configuration

The API reads its settings from `api/src/Tides.Api/appsettings.json`. In the Development environment, `appsettings.Development.json` overrides them. Environment variables override both, with `__` in place of `:`, so `Kartverket:BaseAddress` becomes `Kartverket__BaseAddress`. A list takes one variable per item, numbered from 0, so the first allowed origin is `Cors__AllowedOrigins__0`.

| Setting | Default | What it does |
| --- | --- | --- |
| `Kartverket:BaseAddress` | `https://vannstand.kartverket.no/` | Address of Kartverket's Tide API. The API does not start unless it is an absolute http or https address. |
| `Cors:AllowedOrigins` | Empty, and `http://localhost:5173` in Development | Browser origins allowed to call the API's GET endpoints from another origin. Each origin is a scheme, host and port with no trailing slash. When the list is empty, browsers block all cross-origin requests. |
