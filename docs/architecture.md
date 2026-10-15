# Architecture

The system being built, shown as a context diagram and two container diagrams from the [C4 model](https://c4model.com/). One shows the containers that run today, and the other shows the containers planned once SQL Server and sign-in are added.

## Context

```mermaid
flowchart TB
    visitor(["Visitor<br/>views tide stations and tides"])
    admin(["Admin<br/>manages locations and imports"])
    game["Game client<br/>reads tide data"]

    tides["<b>Tides</b><br/>React app + .NET API + SQL Server"]

    tideApi["Kartverket Tide API<br/>tide data, XML"]
    tiles["Kartverket map tiles<br/>topo and nautical"]
    baseMap["OpenFreeMap<br/>base map tiles"]
    entra["Microsoft Entra ID<br/>sign-in and tokens"]

    visitor -- "uses, no sign-in" --> tides
    admin -- "signs in, manages data" --> tides
    game -- "reads, public API" --> tides
    tides -- "imports tide data" --> tideApi
    tides -- "loads map tiles" --> tiles
    tides -- "loads base map" --> baseMap
    tides -- "authenticates admins" --> entra
```

| Element | Role |
| --- | --- |
| Visitor | Anyone. Reads tide data without signing in. |
| Admin | Signs in with Entra ID and needs the `Admin` role to manage locations and imports. |
| Game client | Reads tide data through the public, versioned API. Holds no keys or secrets. |
| Tides | Imports tide data from Kartverket, stores it and serves it as JSON. |
| Kartverket Tide API | Source of tide data. Free, no key, licensed CC BY 4.0. |
| Kartverket map tiles | Topo map and nautical chart drawn over the base map, loaded directly by the browser. Free, no key. |
| OpenFreeMap | Base map under the Kartverket tiles, from OpenStreetMap data. Shows where Kartverket has no coverage or its tiles fail to load. Free, no key. |
| Microsoft Entra ID | Signs in admins and issues the tokens the API checks. |

## Containers today

```mermaid
---
config:
  layout: elk
  elk:
    mergeEdges: false
    nodePlacementStrategy: BRANDES_KOEPF
---
flowchart TB
    visitor(["<b>Visitor</b>"])

    spa["<b>React app</b><br/>TypeScript, MapLibre"]:::tides
    vite["<b>Vite dev server</b><br/>proxies /health and /v1"]:::tides
    api["<b>API</b><br/>.NET 10, stations cached in memory"]:::tides

    tiles["<b>Kartverket tiles</b><br/>topo and nautical"]:::ext
    baseMap["<b>OpenFreeMap</b><br/>base map"]:::ext
    tideApi["<b>Kartverket Tide API</b><br/>station list, XML"]:::ext

    visitor -- "views<br/>tide stations" --> spa
    spa -- "loads the app,<br/>GET /v1/stations" --> vite
    vite -- "proxies" --> api
    spa -- "loads tiles" --> tiles
    spa -- "loads<br/>base map" --> baseMap
    api -- "fetches<br/>station list" --> tideApi

    classDef tides stroke-width:2px
    classDef ext stroke-dasharray:4 3
```

| Container | Role |
| --- | --- |
| React app | Shows the map and Kartverket's tide stations as markers. Loads map tiles directly and fetches stations from the API. |
| Vite dev server | Serves the React app in development and proxies `/health` and `/v1` to the API, so the browser makes no cross-origin requests. |
| API | Serves the station list as JSON. Fetches it from Kartverket on the first request and keeps it in memory for 24 hours. |

## Containers planned

```mermaid
---
config:
  layout: elk
  elk:
    mergeEdges: false
    nodePlacementStrategy: BRANDES_KOEPF
---
flowchart TB
    visitor(["<b>Visitor</b>"])
    admin(["<b>Admin</b><br/>manages data"])
    game["<b>Game client</b>"]

    spa["<b>React app</b><br/>TypeScript, MapLibre, MSAL<br/>installable PWA"]:::tides
    api["<b>API</b><br/>.NET 10, EF Core"]:::tides
    db[("<b>Database</b><br/>SQL Server")]:::tides

    tiles["<b>Kartverket tiles</b><br/>topo and nautical"]:::ext
    baseMap["<b>OpenFreeMap</b><br/>base map"]:::ext
    entra["<b>Microsoft Entra ID</b><br/>sign-in and tokens"]:::ext
    tideApi["<b>Kartverket Tide API</b><br/>tide data, XML"]:::ext

    visitor -- "views<br/>tide data" --> spa
    admin -- "manages<br/>data" --> spa
    game -- "reads JSON" --> api
    spa -- "HTTPS,<br/>JSON" --> api
    spa -- "loads tiles" --> tiles
    spa -- "loads<br/>base map" --> baseMap
    spa -- "signs in admins" --> entra
    api -- "validates tokens" --> entra
    api -- "imports" --> tideApi
    api -- "EF Core" --> db

    classDef tides stroke-width:2px
    classDef ext stroke-dasharray:4 3
```

| Container | Role | Hosted on |
| --- | --- | --- |
| React app | Shows stations and tides on the map. Admins sign in with MSAL and send their changes to the API with an access token. | Azure Static Web Apps |
| API | Serves tide data as JSON. Checks the `Admin` role on every write and runs imports from Kartverket. | Azure Container Apps, image from GitHub Container Registry |
| Database | Stores locations, imports and the imported tide data. | SQL Server in Docker locally, Azure SQL in the cloud |
