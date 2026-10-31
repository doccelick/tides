# Testing

The API has unit tests and integration tests, both written with xUnit v3. The frontend has Vitest tests. CI runs all of them on every pull request, as described in [development.md](development.md#continuous-integration).

## Run the tests

From the repository root:

```shell
dotnet test api/Tides.slnx
```

`global.json` switches `dotnet test` to Microsoft.Testing.Platform.

The integration tests start SQL Server in a Docker container, so Docker must be running. The first run pulls the SQL Server image, which takes a few minutes. To run only the unit tests, without Docker:

```shell
dotnet test api/tests/Tides.Api.Tests
```

To run only the tests whose method names match a pattern:

```shell
dotnet test --project api/tests/Tides.Api.IntegrationTests --filter-method "*NoCache*"
```

For the frontend, from `frontend/`:

```shell
npm run test
```

`npm run test:watch` runs the tests again when a file changes.

## API unit tests

`api/tests/Tides.Api.Tests` tests the API without a database.

- Parser tests read sample Kartverket responses from XML files in `Kartverket/TestData`, which the build copies to the output directory. The files cover the station list, tides, predictions and observations, and Kartverket's no-data and error responses.
- Endpoint tests start the API in memory with `WebApplicationFactory<Program>`. `ConfigureTestServices` gives the Kartverket client a fake HTTP handler that returns the XML files, and replaces `TimeProvider` with `FakeTimeProvider`. A test therefore controls what Kartverket returns and what time it is. The tests check status codes, bodies, cache headers and how often the API calls Kartverket.
- Logic that needs no host, such as the distance between two positions and the gaps between imports, is tested directly.

## API integration tests

`api/tests/Tides.Api.IntegrationTests` tests the code that uses the database against a real SQL Server.

- `SqlServerFixture` is an xUnit assembly fixture. It starts one SQL Server container with Testcontainers for the whole test run, from the same image as `compose.yml`, and applies the EF Core migrations to it. The container is removed when the run ends.
- `TidesApiFactory` starts the API in memory against that database. It takes an optional fake Kartverket handler and an optional `TimeProvider`. `AddLocationAsync` saves a location for a test to use.
- `FakeKartverketHandler` answers tide and prediction requests with the XML in `KartverketXml` and counts the requests. A test can then check whether the API read from the database or called Kartverket.
- `TestTokens` signs short-lived access tokens with a random key, with the claims Entra puts in tokens for the API and optional roles. The factory makes the API trust that key and issuer and download nothing from Entra, while Microsoft.Identity.Web still checks the audience and the claims. `CreateSignedInClient("Admin")` returns a client that sends such a token, and `CreateSignedInClient()` one whose token has no role.
- Endpoint tests send HTTP requests through the factory's client. Service and `TidesDbContext` tests get their objects from a scope on the factory's services and call them directly.
- All tests share the one database, and nothing is cleared between tests. Each test creates the locations it needs and checks only its own rows.

## Frontend tests

Vitest runs the tests in jsdom, configured under `test` in `frontend/vite.config.ts`. Test files sit next to the code they test, as `*.test.ts` or `*.test.tsx`.

- Components are rendered with React Testing Library and checked through roles, labels and text, using the jest-dom matchers. `userEvent` drives clicks and typing.
- Hooks are tested with `renderHook`.
- Tests replace `fetch` with `vi.stubGlobal` and return `Response` objects, so no API has to run. `unstubGlobals` restores every stubbed global after each test.
- No test signs in. Tests of the admin tools replace `useIsAdmin` with `vi.mock`, and tests that check the bearer header replace `getAdminToken` with one that returns a fixed token.
- `src/test/setup.ts` adds the jest-dom matchers and cleans up after each test. jsdom has no `IntersectionObserver`, so the setup gives each test one that does nothing. Tests that need visibility changes stub their own.
- Tests run with `TZ=Europe/Oslo`, so times are formatted the same on every machine.
