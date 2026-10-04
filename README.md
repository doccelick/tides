# Tides

Imports tide data from Kartverket's public API, stores it in SQL Server and serves it as JSON through a versioned .NET 10 minimal API. A React frontend shows tide stations and forecasts on a map, and an admin signs in with Entra ID to manage locations and imports. The project is built to practice production skills utilizing containers, automated tests, CI/CD with GitHub Actions, Azure deployment and keyless authentication throughout.

## Quick start

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```shell
docker compose up --watch
```

Open `http://localhost:5173`, or browse the API at `http://localhost:8080/scalar/v1`. See [docs/development.md](docs/development.md) for running without Docker and for tests.

## Documentation

| Topic | File |
| --- | --- |
| Development | [docs/development.md](docs/development.md) |
| Architecture | [docs/architecture.md](docs/architecture.md) |
| Code guidelines | [docs/guidelines.md](docs/guidelines.md) |
| Architecture decisions | [docs/adr/](docs/adr/) |

## License

Code is licensed under the [MIT License](LICENSE).

Tide data and map tiles come from [Kartverket](https://www.kartverket.no/) through the [Tide API](https://vannstand.kartverket.no/tideapi_en.html), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Not suitable for navigation.
