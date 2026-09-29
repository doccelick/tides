# Architecture

The system being built, shown as a context diagram: Tides and the people and systems it interacts with.

## Context

```mermaid
flowchart TB
    visitor(["Visitor<br/>views tide stations and tides"])
    admin(["Admin<br/>manages locations and imports"])
    game["Game client<br/>reads tide data"]

    tides["<b>Tides</b><br/>React app + .NET API + SQL Server"]

    tideApi["Kartverket Tide API<br/>tide data, XML"]
    tiles["Kartverket map tiles<br/>topo and nautical"]
    entra["Microsoft Entra ID<br/>sign-in and tokens"]

    visitor -- "uses, no sign-in" --> tides
    admin -- "signs in, manages data" --> tides
    game -- "reads, public API" --> tides
    tides -- "imports tide data" --> tideApi
    tides -- "loads map tiles" --> tiles
    tides -- "authenticates admins" --> entra
```

| Element | Role |
| --- | --- |
| Visitor | Anyone. Reads tide data without signing in. |
| Admin | Signs in with Entra ID and needs the `Admin` role to manage locations and imports. |
| Game client | Reads tide data through the public, versioned API. Holds no keys or secrets. |
| Tides | Imports tide data from Kartverket, stores it and serves it as JSON. |
| Kartverket Tide API | Source of tide data. Free, no key, licensed CC BY 4.0. |
| Kartverket map tiles | Map backgrounds, loaded directly by the browser. |
| Microsoft Entra ID | Signs in admins and issues the tokens the API checks. |
