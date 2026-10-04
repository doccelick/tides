# Configuration

The API reads its settings from `api/src/Tides.Api/appsettings.json`. Environment variables override them, with `__` in place of `:`, so `Kartverket:BaseAddress` becomes `Kartverket__BaseAddress`.

| Setting | Default | What it does |
| --- | --- | --- |
| `Kartverket:BaseAddress` | `https://vannstand.kartverket.no/` | Address of Kartverket's Tide API. The API does not start unless it is an absolute http or https address. |
