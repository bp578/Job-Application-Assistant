# Decisions Log

Short records of design choices and why they were made. Each one is an answer to "why did you build it that way?"

## 001: Use official job-board APIs, not scraping
**Date:** 2026-11-17
**Decision:** Pull postings from Greenhouse's Job Board API and Lever's Postings API. Don't scrape LinkedIn or Indeed.
**Why:** Both APIs are public, documented, and meant for exactly this. They return structured JSON, so there are no brittle HTML selectors, and they don't break a site's terms of service. LinkedIn and Indeed prohibit automated access. Many target companies (Anthropic, Pinterest, Palantir) post through Greenhouse or Lever.
**Trade-off:** Companies on Workday or other systems aren't covered yet. Ashby is a candidate to add next.

## 002: One client per job board behind an interface
**Date:** 2026-11-17
**Decision:** `IJobBoardClient` with `GreenhouseClient` and `LeverClient`; the ingestion service picks a client by each company's board.
**Why:** Adding a new board means writing one class, with no changes to ingestion logic. Fake clients make the ingestion service easy to unit test without network calls.

## 003: Upsert by (CompanyId, ExternalId); one failing company doesn't stop the run
**Date:** 2026-11-17
**Decision:** A unique index on (CompanyId, ExternalId). Existing postings are updated and get a new `LastSeenAt`; new ones are inserted. Network or JSON errors are caught per company and reported in the result.
**Why:** Ingestion can run any number of times without duplicating rows. `LastSeenAt` going stale shows a posting was taken down. One bad board token shouldn't block the rest of the watchlist.

## 004: EnsureCreated for Phase 1, migrations later
**Date:** 2026-11-17
**Decision:** Create the schema with `EnsureCreated` on startup.
**Why:** Fastest path to a running app while the schema changes daily.
**Revisit:** Switch to EF Core migrations before Phase 2 adds screening tables. `EnsureCreated` can't evolve an existing schema.

## 005: Vite dev proxy instead of CORS
**Date:** 2026-11-17
**Decision:** The React dev server forwards `/api` to the API.
**Why:** The browser sees a single origin, so no CORS policy is needed in development. In production the API will serve the built front end or sit behind the same host.
