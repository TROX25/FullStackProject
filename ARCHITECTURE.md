# Architecture

## Components

```
                          ┌─────────────────────────┐
                          │   mojprojekt-client      │
                          │   (Angular 21, standalone │
                          │    components, signals)   │
                          └────────────┬──────────────┘
                                       │ HTTP (JSON)
                          ┌────────────▼──────────────┐
                          │       MojProjekt.Api       │  composition root:
                          │  minimal API endpoints,    │  DI wiring, migrate-on-
                          │  CORS, OpenAPI              │  startup, endpoint mapping
                          └────────────┬──────────────┘
                                       │
                          ┌────────────▼──────────────┐
                          │   MojProjekt.Application    │  CQRS (MediatR): commands/
                          │   (no EF/HTTP/AI deps)      │  queries, DTOs, interfaces
                          └────────────┬──────────────┘
                                       │ implements
                          ┌────────────▼──────────────┐
                          │  MojProjekt.Infrastructure  │  EF Core + SQLite, OLX
                          │                              │  crawler, Anthropic client,
                          │                              │  deterministic ranking
                          └────────────┬──────────────┘
                                       │
                          ┌────────────▼──────────────┐
                          │      MojProjekt.Domain      │  Listing, CrawlRun, enums,
                          │      (no dependencies)      │  Money value object
                          └────────────────────────────┘
```

Dependency direction is enforced purely by project references: `Domain` has zero outward dependencies; `Application` depends only on `Domain`; `Infrastructure` implements `Application`'s interfaces and depends on both; `Api` composes everything via DI. This means it's structurally impossible for `Domain`/`Application` to reference EF Core, AngleSharp, or the Anthropic HTTP client — those concerns can only live in `Infrastructure`.

## CQRS

Commands/queries use [MediatR](https://github.com/jbogard/MediatR) rather than a hand-rolled mediator. Each feature (`Listings`, `Crawling`, `Search`) has its own folder under `Application/` with a command/query record, a DTO, and a handler that depends only on interfaces (`IListingRepository`, `IListingCrawler`, `INaturalLanguageQueryInterpreter`, `IListingRankingService`) — never on `Infrastructure` types directly.

## Data flow: crawling

1. `POST /api/crawl` → `TriggerCrawlCommand` creates a `CrawlRun` row (`Pending`) and returns its id immediately (`202 Accepted`) — the frontend polls `GET /api/crawl/{id}` (or `/latest`) rather than holding the HTTP connection open for the whole crawl.
2. The actual crawl runs on a background `Task` (`CrawlRunner`, using its own DI scope so it gets its own `DbContext`), and marks the run `Running` → `Completed`/`Failed`.
3. `IListingCrawler` is resolved as `CompositeListingCrawler`, a decorator that:
   - In `Auto` mode (default): tries the live `OlxCrawler` first, and **automatically falls back** to `SampleDataCrawler` (a bundled, realistic ~100-listing fixture) if the live crawl throws `CrawlUnavailableException` or returns zero listings.
   - In `SampleOnly` mode: always uses the sample dataset — for a guaranteed deterministic demo/tests.
   - In `LiveOnly` mode: never falls back — surfaces failures directly.
   - `CrawlResult.SourceUsed` (`Live`/`Fallback`) is always recorded on the `CrawlRun` and surfaced in the UI's status banner, so the fallback is never silent.
4. `OlxCrawler` fetches OLX.pl's "Motoryzacja" (cars) search-results pages (newest first), checks `robots.txt` before requesting anything, applies a rate-limit delay (`Crawl-delay` from robots.txt if present, else a fixed delay), filters cards to those published within the requested age window (24h) using OLX's own relative-time labels ("3 godziny temu", "Dzisiaj o…"), then fetches detail pages only for in-window listings (capped) and parses them via `OlxHtmlParser` — a pure, HTTP-free class kept separate specifically so it can be unit-tested against saved fixture HTML.
5. Listings are upserted by `(Source, SourceListingId)` (unique index) so re-crawling doesn't create duplicates.

**Caveat on the live crawler**: this project was built in a sandboxed environment with no general outbound internet access (only specific package registries and the Anthropic API were reachable), so `OlxCrawler`'s selectors could only be validated against a synthetic fixture approximating OLX's historically documented markup — never against the live site. See `ASSUMPTIONS.md`. The automatic fallback means the app is fully usable either way, but the live path should be re-verified against the real site before relying on it for a demo.

## Data flow: search

`POST /api/search` → `SearchListingsQuery` handler orchestrates three independent, swappable pieces:

1. **`INaturalLanguageQueryInterpreter`** (`AnthropicQueryInterpreter`) turns the raw query into structured `SearchCriteria` (price range, year range, mileage, transmission/fuel/body-type *preferences* — each with an `IsRequired` flag distinguishing "must be automatic" from "preferably automatic" — brand, model, free-text keywords) plus a plain-English `IntentSummary`.
   - Uses Claude's **tool-use (forced function calling)** against a strict JSON schema (`extract_search_criteria`) rather than asking for free-form JSON in a prompt — this is the concrete reliability guardrail.
   - The raw tool output is run through `CriteriaValidator`, which clamps/rejects implausible values (negative prices, years outside 1970–next year, unrecognized enum strings) rather than trusting the model blindly, recording a warning for anything it had to adjust.
   - If no API key is configured, the call fails, times out, or the model doesn't invoke the tool (retried once), it falls back to `NaiveCriteriaExtractor` — a small regex/keyword extractor — so search always stays functional, never hard-failing on a missing key or a flaky AI call. This is surfaced via the response's `warnings` array, never silently.
2. **`IListingRepository`** loads the full local dataset (`GetAllAsync`) — fine at this project's scale (dozens to low hundreds of rows); a larger dataset would need a real prefilter, called out in `ASSUMPTIONS.md`.
3. **`IListingRankingService`** (`ListingRankingService`) scores every listing deterministically: budget fit, year fit, mileage fit, and soft-vs-hard handling of transmission/fuel/body-type preferences (a hard "must" that isn't met excludes the listing; a soft "preferably" mismatch just costs points and is recorded), brand/model match, and free-text keyword overlap against title/description. **This is the key explainability decision**: match reasons and unmet preferences are produced by this deterministic, fully unit-tested code — never by a second AI call — so "why this matched" is always traceable to a specific rule in `ListingRankingServiceTests`, not a hallucinated explanation.

Embeddings/semantic similarity were deliberately not added — see "Key decisions" below.

## Database

SQLite via EF Core, file at `MojProjekt.Api/App_Data/mojprojekt.db` (gitignored), with `Database.Migrate()` run on every startup so a fresh checkout needs zero manual DB setup. WAL journal mode is enabled after migration so concurrent reads (search, browsing) aren't blocked behind a crawl's write transaction. There is **no migration-seeded data** — the crawl pipeline (live or fallback) is the only way rows get into the database, which means every local run naturally exercises the required fallback behavior instead of it being a separate, easy-to-forget code path.

## Key decisions

| Decision | Rationale |
|---|---|
| MediatR for CQRS | Standard, widely recognized .NET choice; less hand-rolled plumbing than a custom mediator. (Its license is free for a project at this scale — worth being aware of if this ever became a commercial product, see `ASSUMPTIONS.md`.) |
| SQLite, no Docker | Keeps "must run locally" frictionless — zero external services to install/start. |
| Deterministic scorer produces match explanations, not a second AI call | Explanations are then always traceable to code and unit-testable, never hallucinated — directly serves the assignment's "make the matching logic understandable" requirement. |
| No embeddings/semantic search in this version | At this dataset size (~100 listings), keyword/structured matching is sufficient and stays fully explainable; adding a vector store would increase cost/latency/complexity for limited benefit here. Listed as a "what I'd improve" item. |
| Automatic live→fallback crawler decorator | Satisfies the assignment's explicit allowance for "a mocked or sample fallback… when clearly explained and the real integration is still demonstrated" — the demo works regardless of the live site's current anti-bot posture, and the fact/source is never hidden from the user. |
| API-triggered crawl (background `Task`) rather than a scheduled job | Simplest approach that's still fully defensible for something evaluated on a working end-to-end flow; a real recurring `IHostedService`/scheduler is a "what I'd improve" item. |
