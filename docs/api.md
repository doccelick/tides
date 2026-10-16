# API

The API serves JSON over HTTP. Endpoints are versioned under `/v1`, and none of them require authentication. They only read data that Kartverket publishes openly under CC BY 4.0, so there is nothing to protect.

When run locally, the API listens on `http://localhost:8080`. See [development.md](development.md) for how to start it.

## Endpoints

| Method and path | Returns |
| --- | --- |
| `GET /health` | `Healthy` as plain text when the API is up |
| `GET /v1/stations` | Kartverket's permanent tide stations |

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

## Caching

`GET /v1/stations` responds with `Cache-Control: public, max-age=3600` and an `ETag`. A client that sends the `ETag` back in `If-None-Match` gets `304 Not Modified` with no body while the station list is unchanged.

## Errors

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) with the `application/problem+json` content type.

| Status | When |
| --- | --- |
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
