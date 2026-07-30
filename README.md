# AI-Powered Automotive Listings Search

A small fullstack app that collects recently published car listings from OLX.pl, stores them locally, and lets a user search them in natural language (e.g. *"a reliable family estate under PLN 60,000, preferably automatic and no older than 2019"*), with every result explaining **why** it matched.

Built for a recruitment take-home assignment. See also:
- [`ARCHITECTURE.md`](./ARCHITECTURE.md) — components, data flow, key design decisions
- [`ASSUMPTIONS.md`](./ASSUMPTIONS.md) — assumptions, known limitations, what's next
- [`AI-WORKFLOW.md`](./AI-WORKFLOW.md) — how this was built with AI assistance

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm
- An [Anthropic API key](https://console.anthropic.com/) — **optional**. Without one, natural-language search still works via a simplified keyword-based fallback (see `AI-WORKFLOW.md`); the AI-powered interpretation just won't be used.

No Docker and no separate database server are required — the backend uses a local SQLite file created automatically on first run.

## 1. Backend setup

```bash
cd MojProjekt.Api
dotnet restore
```

### Supply your Anthropic API key (optional but recommended)

Pick one:

```bash
# Option A — .NET user-secrets (recommended for local dev, never committed)
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."

# Option B — environment variable
export ANTHROPIC_API_KEY="sk-ant-..."
```

Never put a real key in `appsettings.json` or `appsettings.Development.json` and commit it — `appsettings.Development.json` is already gitignored for this reason.

### Run it

```bash
dotnet run --project MojProjekt.Api
```

This starts the API on `http://localhost:5025` (see `MojProjekt.Api/Properties/launchSettings.json`). On startup it:
1. Creates `MojProjekt.Api/App_Data/` if missing.
2. Applies EF Core migrations, creating `App_Data/mojprojekt.db` (a fresh SQLite file) if it doesn't exist yet.

The database starts **empty** — see step 3 below to populate it.

### Forcing an offline, deterministic demo

By default the crawler runs in `Auto` mode: it tries live OLX crawling first and automatically falls back to a bundled sample dataset if that's unavailable (see `ARCHITECTURE.md`). To skip the live attempt entirely (useful for an interview demo, CI, or if you have no network access), set:

```bash
export Crawler__Mode=SampleOnly
```

before `dotnet run`, or edit `"Crawler": { "Mode": "SampleOnly" }` in `appsettings.json`.

## 2. Frontend setup

```bash
cd mojprojekt-client
npm install
npm start   # ng serve, defaults to http://localhost:4200
```

The Angular app expects the backend at `http://localhost:5025/api` (see `src/app/core/services/api-config.ts`). Adjust that constant if you run the backend on a different port/profile.

## 3. First-use flow

1. With both servers running, open `http://localhost:4200`.
2. Click **"Fetch listings"** in the banner at the top — this triggers a crawl (`POST /api/crawl`) and polls until it completes. The banner will show whether it used **Live OLX data** or **Sample fallback data**.
3. Type a search query (or click one of the example chips) and press **Search**.
4. Click **"Why this matched"** on any result to see the specific reasons it was ranked where it was, and any preferences it didn't fully meet.

## Running the tests

```bash
# Backend — unit, crawler/parsing, and API integration tests
cd /path/to/repo
dotnet test

# Frontend — component/service tests (Vitest)
cd mojprojekt-client
npm test
```

## Resetting the local database

Stop the backend and delete `MojProjekt.Api/App_Data/mojprojekt.db` (plus any `-shm`/`-wal` files next to it). It will be recreated empty on the next `dotnet run`.

## Project layout

```
MojProjekt.Domain/            Entities, value objects, enums — no outward dependencies
MojProjekt.Application/       CQRS (MediatR) commands/queries, DTOs, interfaces implemented by Infrastructure
MojProjekt.Infrastructure/    EF Core/SQLite, OLX crawler, Anthropic integration, ranking service
MojProjekt.Api/               ASP.NET Core minimal API — composition root
MojProjekt.UnitTests/         Ranking, AI-parsing/validation, naive extractor tests
MojProjekt.Infrastructure.Tests/  Crawler HTML parsing (fixtures) and robots.txt parsing tests
MojProjekt.Api.IntegrationTests/  WebApplicationFactory-based end-to-end API tests
mojprojekt-client/             Angular 21 standalone-components frontend
```

## EF Core migrations (if you change the data model)

```bash
dotnet tool install --global dotnet-ef   # once
dotnet ef migrations add <Name> --project MojProjekt.Infrastructure --startup-project MojProjekt.Infrastructure
```

Migrations are applied automatically on API startup, so no separate `dotnet ef database update` step is needed for normal use.
