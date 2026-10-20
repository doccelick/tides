# API

The API serves JSON over HTTP. Endpoints are versioned under `/v1`, and none of them require authentication. They only read data that Kartverket publishes openly under CC BY 4.0, so there is nothing to protect.

When run locally, the API listens on `http://localhost:8080`. See [development.md](development.md) for how to start it.

## Endpoints

| Method and path | Returns |
| --- | --- |
| `GET /health` | `Healthy` as plain text when the API is up |
| `GET /v1/stations` | Kartverket's permanent tide stations |
| `GET /v1/stations/{code}/tides` | High and low tides at a station for today and tomorrow (UTC) |
| `GET /v1/stations/{code}/water-level` | The latest observed water level at a station |

## Stations

`GET /v1/stations` returns every permanent tide station with its code, name and position in decimal degrees.

```json
{
  "stations": [
    { "code": "BGO", "name": "Bergen", "latitude": 60.398046, "longitude": 5.320487 },
    { "code": "BOO", "name": "Bodø", "latitude": 67.29233, "longitude": 14.39977 }
  ]
}
```

The API fetches the station list from Kartverket on the first request and keeps it in memory for 24 hours.

## Tides

`GET /v1/stations/{code}/tides` returns the high and low tides at a station for today and tomorrow, counted in UTC. The station code is not case-sensitive, so `boo` and `BOO` give the same result.

```json
{
  "stationCode": "BOO",
  "timeZone": "UTC",
  "unit": "cm",
  "reference": "CD",
  "tides": [
    { "type": "High", "time": "2026-10-18T03:48:00+00:00", "levelCm": 222.9 },
    { "type": "Low", "time": "2026-10-18T09:45:00+00:00", "levelCm": 142.6 }
  ]
}
```

Times are in UTC. Levels are in centimetres above chart datum (CD), the reference level used on Norwegian nautical charts and close to the lowest astronomical tide. A level is rarely negative, and only when the water falls below chart datum.

The API fetches a station's tides from Kartverket on the first request each UTC day, and answers the rest of that day's requests for the station from memory.

## Water level

`GET /v1/stations/{code}/water-level` returns the latest water level measured at a station. Kartverket records a level every 10 minutes, and each one is published 10 to 20 minutes after it was measured.

```json
{
  "stationCode": "BOO",
  "timeZone": "UTC",
  "unit": "cm",
  "reference": "CD",
  "time": "2026-10-20T20:10:00+00:00",
  "levelCm": 207.9
}
```

`time` is when the level was measured, in UTC. The level is in centimetres above chart datum, like the tides. It differs from the predicted tide when weather pushes the water up or down.

The API returns a level only if it was at most 30 minutes old when the API fetched it from Kartverket. It keeps a station's level in memory for 5 minutes, and browsers may keep the response for another 5, so a level can be up to 40 minutes old when shown.

## Caching

`GET /v1/stations` and `GET /v1/stations/{code}/tides` respond with `Cache-Control: public, max-age=3600` and an `ETag`. `GET /v1/stations/{code}/water-level` changes more often and responds with `Cache-Control: public, max-age=300` and an `ETag`. A client that sends the `ETag` back in `If-None-Match` gets `304 Not Modified` with no body while the data is unchanged.

## Errors

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) with the `application/problem+json` content type.

| Status | When |
| --- | --- |
| `404 Not Found` | The station code is unknown. The title is `Station not found.` |
| `404 Not Found` | Kartverket has no tide data for the station. The title is `No tide data for this station.` |
| `404 Not Found` | Kartverket has no water level for the station from the last 30 minutes. The title is `No water level for this station.` |
| `502 Bad Gateway` | Kartverket's Tide API fails, times out or returns a response the API cannot read |

## Cross-origin requests

Browsers on another origin can call the GET endpoints when their origin is in `Cors:AllowedOrigins`, described in [configuration.md](configuration.md). They may send `If-None-Match`, and can read the `ETag` response header.

## OpenAPI and Scalar

In the Development environment, the API serves its OpenAPI document and a Scalar UI for browsing and calling the endpoints. Both are off in Production.

| URL | What it is |
| --- | --- |
| `http://localhost:8080/openapi/v1.json` | OpenAPI document |
| `http://localhost:8080/scalar/v1` | Scalar UI |

The same document is committed as `api/src/Tides.Api/openapi.json`, and the frontend's TypeScript types are generated from it. See [development.md](development.md#openapi-document).
