# Assumptions, limitations, and what I'd improve

## Assumptions

- **Portal scope**: only OLX.pl's "Motoryzacja" (passenger cars) section is targeted; other categories/portals aren't crawled.
- **Currency**: prices are assumed to be PLN throughout (OLX.pl cars listings are overwhelmingly PLN-denominated); no multi-currency conversion.
- **"Last 24 hours" freshness**: derived from OLX's own relative-time labels on the search-results cards (e.g. "3 godziny temu", "Dzisiaj o 14:20"), not a precise server timestamp — this is an approximation, not a guarantee, and listings phrased as an absolute calendar date are treated as outside the window rather than guessed at.
- **No authentication/multi-user**: the app is single-user/local by design, matching the assignment's local-execution focus.
- **Ranking weights are hand-tuned heuristics** (`ListingRankingService`), not learned from data or user feedback.
- **The AI query interpreter only ever adds/refines *criteria*** — it never fabricates or edits listing data, and it never generates the "why this matched" text itself (see `ARCHITECTURE.md`).

## A known and important limitation of this specific build

This project was developed inside a **sandboxed environment with no general outbound internet access** — only specific package registries (NuGet, npm) and the Anthropic API were reachable; requests to `olx.pl` and even plain test domains like `example.com` were rejected by the environment's network policy (confirmed via direct `curl` tests). As a direct consequence:

- `OlxCrawler`/`OlxHtmlParser`'s CSS selectors are a best-effort based on OLX's historically documented markup conventions (`data-cy`/`data-testid` attributes), encoded into a **synthetic fixture** for testing — they were **never validated against the real, current OLX.pl HTML**.
- **Before relying on the live crawl path for a demo or interview**, run it once against the real site (`Crawler:Mode=Auto` or `LiveOnly`) and check the logs / `GET /api/crawl/latest`'s `sourceUsed` field. If OLX's markup has changed (or its anti-bot measures block the request outright — plausible for any major classifieds site), the selectors in `OlxHtmlParser` will need adjusting; the class-level doc comment there marks exactly where.
- The automatic fallback to the bundled sample dataset means the application is fully functional and demoable either way — this was a deliberate design choice specifically to de-risk this exact scenario (see `ARCHITECTURE.md`'s "Key decisions").
- Similarly, the `AnthropicQueryInterpreter` code path could be reached (the sandbox could call `api.anthropic.com`) but was only exercised with a **mocked HTTP handler** in unit tests, never a real API key — I don't have one to test with in this environment. The no-key fallback path (`NaiveCriteriaExtractor`) was verified end-to-end, including in the browser.

## Other known limitations

- **Search loads the entire local dataset into memory** for scoring (`IListingRepository.GetAllAsync`). Fine at the dozens-to-low-hundreds scale this project targets; would need a real SQL-level prefilter (or a proper search index) at larger scale.
- **No caching of repeated identical AI queries** — every search re-calls Claude (when configured), even for an identical query moments apart.
- **No scheduled/recurring crawling** — a crawl only happens when a user clicks "Fetch listings" (`POST /api/crawl`); there's no background job keeping the dataset continuously fresh.
- **Image handling is minimal** — only the first thumbnail is shown per result card; no gallery/lightbox.
- **No end-to-end (Cypress/Playwright) test suite committed to the repo** — the UI's golden path was verified manually via a one-off Playwright script during development (see `AI-WORKFLOW.md`), but that script wasn't kept as a maintained, repo-committed test.
- **MediatR's license** is free for a project at this scale (personal/portfolio), but its commercial-use licensing above certain team/revenue thresholds is worth being aware of if this pattern were reused in a real product — flagged here rather than silently assumed away.
- **`GET /api/listings`/`/api/listings/{id}` have no dedicated automated tests** beyond the integration test covering the paged-listing happy path — lower priority than the AI/ranking/crawler logic given the assignment's evaluation focus.

## What I'd improve with more time

1. **Verify and harden the live OLX crawler** against the real site — the single highest-value next step, given the sandbox constraint above.
2. **Add a semantic/embedding layer** as a genuinely optional enhancement on top of the structured criteria — e.g. to catch a query like "something sporty" that doesn't map cleanly to a body-type enum — while keeping the deterministic scorer as the primary, explainable mechanism.
3. **Scheduled recurring crawling** (a real `IHostedService`/Quartz job) instead of manual trigger-only.
4. **Cache identical/near-identical AI query interpretations** to cut latency and cost.
5. **Broaden vehicle coverage** (motorcycles, commercial vehicles) and more granular body-type/fuel-type taxonomies if the target audience needed them.
6. **A committed Playwright/Cypress e2e suite** covering the crawl → search → "why this matched" flow in a real browser, instead of the one-off manual verification script used during development.
7. **Pagination/infinite scroll** on the plain "browse all listings" view once result counts grow beyond a single page.

## Privacy and cost notes (AI usage)

- Only the user's raw search-query text is sent to Anthropic — no listing data, no personal data beyond whatever the user types into the search box.
- Each search issues at most 2 Claude API calls (one retry if the tool isn't invoked on the first attempt), using a small/fast model (`claude-haiku-4-5-20251001` by default, configurable via `Anthropic:Model`) chosen because criteria extraction is a bounded classification/extraction task, not open-ended reasoning — kept deliberately cheap and fast rather than defaulting to a larger model.
- No API key is ever hardcoded, logged, or committed — see the README's "Supply your Anthropic API key" section.
