# API

The API serves JSON over HTTP. Endpoints are versioned under `/v1`. Most of them read tide data that Kartverket publishes openly under CC BY 4.0. The location and import endpoints also save and delete locations and their imported tide data. Saving and deleting need an access token with the `Admin` role, described under [Authentication](#authentication), and every other endpoint is open.

When run locally, the API listens on `http://localhost:8080`. See [development.md](development.md) for how to start it.

## Endpoints

| Method and path | Returns |
| --- | --- |
| `GET /health` | `Healthy` as plain text when the API is up |
| `GET /v1/stations` | Kartverket's permanent tide stations |
| `GET /v1/stations/{code}/tides` | High and low tides at a station for a range of UTC days, today and tomorrow by default |
| `GET /v1/stations/{code}/water-level` | The latest observed water level at a station |
| `POST /v1/locations` | Saves a location near the coast |
| `GET /v1/locations` | All saved locations |
| `DELETE /v1/locations/{id}` | Deletes a location with its imports and their tide data |
| `GET /v1/locations/{id}/tides` | High and low tides at a saved location, with the same range as for stations |
| `GET /v1/locations/{id}/water-level` | An estimated water level at a saved location, based on the nearest station |
| `GET /v1/locations/{id}/samples` | Predicted water levels every 10 or 60 minutes at a saved location, read from its imports |
| `POST /v1/locations/{id}/imports` | Imports a location's tides for a range into the database, skipping what is already imported |
| `GET /v1/locations/{id}/imports` | A location's imports |
| `DELETE /v1/imports/{id}` | Deletes an import and its tide data |

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

## Locations

A location is a named position near the coast, saved in the database. Its tides and water level come from the location endpoints under [Tides](#tides) and [Water level](#water-level-at-a-location), and imports store its tides in the database. [Samples](#samples) reads the imported levels as an evenly spaced series.

`POST /v1/locations` saves a location.

```json
{ "name": "Kjerringøy", "latitude": 67.52, "longitude": 14.76 }
```

`name` is required, at most 100 characters and not only whitespace. `latitude` is from -90 to 90 and `longitude` from -180 to 180, in decimal degrees. Before saving, the API asks Kartverket for the predicted levels at the position for the next hour. Kartverket has no tide data far from the sea, so when it returns none, the API answers `422 Unprocessable Entity` with the title `Too far from the coast.` and saves nothing. Otherwise the response is `201 Created` with the saved location.

```json
{ "id": 7, "name": "Kjerringøy", "latitude": 67.52, "longitude": 14.76 }
```

`GET /v1/locations` returns all saved locations in the same shape, ordered by name.

```json
{
  "locations": [
    { "id": 7, "name": "Kjerringøy", "latitude": 67.52, "longitude": 14.76 }
  ]
}
```

`DELETE /v1/locations/{id}` deletes a location together with its imports and their tide data, and returns `204 No Content`.

## Tides

`GET /v1/stations/{code}/tides` returns the high and low tides at a station for a range of days, counted in UTC. The station code is not case-sensitive, so `boo` and `BOO` give the same result.

| Query parameter | Meaning | Default |
| --- | --- | --- |
| `from` | First day of the range as `YYYY-MM-DD`, in the past or the future | Today (UTC) |
| `days` | Number of days in the range, from 1 to 31 | `2` |

For example, `GET /v1/stations/BOO/tides?from=2026-11-01&days=7` returns the tides from 1 November up to the start of 8 November.

```json
{
  "timeZone": "UTC",
  "unit": "cm",
  "reference": "CD",
  "source": "Kartverket",
  "tides": [
    { "type": "High", "time": "2026-10-18T03:48:00+00:00", "levelCm": 222.9 },
    { "type": "Low", "time": "2026-10-18T09:45:00+00:00", "levelCm": 142.6 }
  ]
}
```

Times are in UTC. Levels are in centimetres above chart datum (CD), the reference level used on Norwegian nautical charts and close to the lowest astronomical tide. A level is rarely negative, and only when the water falls below chart datum.

The API fetches a station's tides for a range from Kartverket on the first request, and answers later requests for the same range from memory for 24 hours. A request without `from` starts on the current UTC day, so it gets a new range when the day changes.

`GET /v1/locations/{id}/tides` returns the same response for a saved location, with the same `from` and `days` parameters. When imports for the location cover the whole range, the API reads the tides from the database. Otherwise it fetches them from Kartverket for the location's position and keeps them in memory for 24 hours, as for stations. `source` in the response is `Database` when the tides were read from imports and `Kartverket` otherwise, so station tides always have `Kartverket`.

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

### Water level at a location

`GET /v1/locations/{id}/water-level` returns an estimated water level at a saved location. Locations have no measurements of their own, so the API estimates the level from the nearest station within 50 km:

1. It takes the station's latest observed level from the last 30 minutes.
2. The weather effect is that level minus the station's predicted level at the same time.
3. The estimate is the location's predicted level at that time plus the weather effect.

```json
{
  "locationId": 7,
  "timeZone": "UTC",
  "unit": "cm",
  "reference": "CD",
  "time": "2026-11-01T00:20:00+00:00",
  "levelCm": 241.0,
  "stationCode": "BOO",
  "stationName": "Bodø",
  "distanceKm": 4.7,
  "weatherEffectCm": 11.0
}
```

`time` is when the station measured its level, so the estimate is for that time and not for the moment of the request. `distanceKm` is the distance from the location to the station. `levelCm`, `distanceKm` and `weatherEffectCm` are rounded to one decimal.

The API keeps a location's estimate in memory for 5 minutes, like a station's level.

## Samples

`GET /v1/locations/{id}/samples` returns a saved location's predicted water level at evenly spaced times for a range of days, counted in UTC. It reads only from the location's imports and never calls Kartverket.

| Query parameter | Meaning | Default |
| --- | --- | --- |
| `from` | First day of the range as `YYYY-MM-DD`, in the past or the future | Today (UTC) |
| `days` | Number of days in the range, from 1 to 31 | `2` |
| `interval` | Minutes between samples, `10` or `60` | `10` |

For example, `GET /v1/locations/7/samples?from=2026-11-01&days=7&interval=60` returns one level per hour from 1 November up to the start of 8 November.

```json
{
  "locationId": 7,
  "timeZone": "UTC",
  "unit": "cm",
  "reference": "CD",
  "intervalMinutes": 60,
  "samples": [
    { "time": "2026-11-01T00:00:00+00:00", "levelCm": 248.0 },
    { "time": "2026-11-01T01:00:00+00:00", "levelCm": 228.0 }
  ]
}
```

Times are in UTC, and levels are in centimetres above chart datum, as for tides. The first sample is at midnight UTC on the first day. Imports store a predicted level every 10 minutes, so a 60-minute interval returns every sixth stored level, without averaging. A 31-day range has 4,464 samples at 10 minutes and 744 at 60 minutes.

When the location's imports don't cover the whole range, the API returns `404 Not Found` and does not fetch the missing part from Kartverket. `GET /v1/locations/{id}/imports` shows which ranges are imported.

## Imports

An import stores a location's tides for a range of time in the database, so `GET /v1/locations/{id}/tides` and `GET /v1/locations/{id}/samples` can read them without calling Kartverket. It holds the high and low tides and the predicted level every 10 minutes.

`POST /v1/locations/{id}/imports` imports the range from `from` up to `to`, where `from` is included and `to` is not. The range can be in the past or the future and at most 31 days long.

```json
{ "from": "2026-11-01T00:00:00+00:00", "to": "2026-11-08T00:00:00+00:00" }
```

The API imports only the parts of the range that no earlier import for the location covers, and each part becomes a separate import. For example, after importing 1 to 8 November, a request for 1 November to 2 December imports 8 November to 2 December. The response is `201 Created` with the new imports.

```json
{
  "imports": [
    {
      "id": 12,
      "locationId": 7,
      "from": "2026-11-08T00:00:00+00:00",
      "to": "2026-12-02T00:00:00+00:00",
      "importedAt": "2026-10-30T18:02:11+00:00",
      "entryCount": 3549
    }
  ]
}
```

`entryCount` is the number of stored high tides, low tides and predicted levels. When imports already cover the whole range, the API returns `409 Conflict` and calls nothing at Kartverket.

`GET /v1/locations/{id}/imports` returns all imports for a location in the same shape, ordered by `from`. `DELETE /v1/imports/{id}` deletes an import with its tide data and returns `204 No Content`.

## Caching

`GET /v1/stations` and `GET /v1/stations/{code}/tides` respond with `Cache-Control: public, max-age=3600` and an `ETag`. `GET /v1/locations/{id}/tides` and `GET /v1/locations/{id}/samples` respond with `Cache-Control: no-cache` and an `ETag`, because adding or deleting an import can change their responses, so clients check with the API each time before they reuse it. Both water level endpoints change more often and respond with `Cache-Control: public, max-age=300` and an `ETag`. A client that sends the `ETag` back in `If-None-Match` gets `304 Not Modified` with no body while the data is unchanged.

## Authentication

`POST /v1/locations`, `DELETE /v1/locations/{id}`, `POST /v1/locations/{id}/imports` and `DELETE /v1/imports/{id}` need a Microsoft Entra ID access token in the `Authorization: Bearer <token>` header. Entra must have issued the token in the tenant set in `AzureAd:TenantId`, for the API registration set in `AzureAd:ClientId`, described in [configuration.md](configuration.md). The token must also carry the `Admin` app role in its `roles` claim. Without a valid token the API answers `401 Unauthorized`, and with a valid token that lacks the role it answers `403 Forbidden`.

## Errors

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) with the `application/problem+json` content type.

| Status | When |
| --- | --- |
| `400 Bad Request` | `from` is not a valid date, `days` is not a whole number from 1 to 31, or `interval` is not `10` or `60`. An invalid `days` or `interval` value is listed under `errors` |
| `401 Unauthorized` | A request to save or delete has no access token, or the token is not valid for the API |
| `403 Forbidden` | A request to save or delete has a valid token without the `Admin` role |
| `404 Not Found` | The station code is unknown. The title is `Station not found.` |
| `404 Not Found` | Kartverket has no tide data for the station. The title is `No tide data for this station.` |
| `404 Not Found` | Kartverket has no water level for the station from the last 30 minutes. The title is `No water level for this station.` |
| `404 Not Found` | The location id is unknown. The title is `Location not found.` |
| `404 Not Found` | Kartverket has no tide data for the location. The title is `No tide data for this location.` |
| `404 Not Found` | The location's imports don't cover the whole range of a samples request. The title is `No imported water levels for this range.` |
| `400 Bad Request` | A location's `name` is missing, only whitespace or longer than 100 characters, or `latitude` or `longitude` is missing or out of range. The field is listed under `errors` |
| `422 Unprocessable Entity` | Kartverket has no tide data at a new location's position. The title is `Too far from the coast.` |
| `400 Bad Request` | An import's `from` or `to` is missing, `to` is not after `from`, or the range is longer than 31 days. The field is listed under `errors` |
| `404 Not Found` | No station is within 50 km of the location, or the nearest one has no water level from the last 30 minutes. The title is `No water level estimate for this location.` |
| `404 Not Found` | `DELETE /v1/locations/{id}` or `DELETE /v1/imports/{id}` has an unknown id |
| `409 Conflict` | Imports already cover the whole range. The title is `Range already imported.` |
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
