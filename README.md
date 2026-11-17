# Job Application Assistant

A personal job-search pipeline: it finds postings from a company watchlist, uses AI to screen them against my background, drafts tailored application material, and pre-fills application forms for me to review before submitting.

**Status:** Phase 1 of 5 (in progress, started Nov 2026).

<!-- Add a demo GIF here after each phase: docs/demo-phase1.gif -->

## Features

| Phase | Feature | Status |
|---|---|---|
| 1 | Ingest postings from Greenhouse and Lever public APIs, store in SQL Server, React job list | 🚧 In progress |
| 2 | AI screening with the Claude API: fit score, matched/missing skills, eligibility knockouts | Planned |
| 3 | Tailored resume content generated only from verified facts, with an eval suite for hallucinations | Planned |
| 4 | Azure deployment with GitHub Actions CI/CD | Planned |
| 5 | Application tracker and Playwright form pre-fill with human review before submit | Planned |

## Tech stack

- **API:** ASP.NET Core 10 minimal APIs, C#
- **Data:** SQL Server 2022, Entity Framework Core
- **Front end:** React 19, TypeScript, Vite
- **Tests:** xUnit
- **Infrastructure:** Docker, Docker Compose

## Architecture

```
React (Vite) ──/api──▶ ASP.NET Core API ──▶ SQL Server
                             │
                             ├─▶ Greenhouse Job Board API
                             └─▶ Lever Postings API
```

- `IJobBoardClient`: one implementation per job board (`GreenhouseClient`, `LeverClient`).
- `IngestionService`: fetches every active company, upserts postings by `(CompanyId, ExternalId)`, and isolates failures per company.
- Design decisions are recorded in [docs/decisions.md](docs/decisions.md).

## Running locally

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download), [Node.js 20+](https://nodejs.org/), [Docker Desktop](https://www.docker.com/products/docker-desktop/)

1. **Set the database password**
   ```powershell
   copy .env.example .env
   # then edit .env and set SA_PASSWORD
   ```

2. **Start SQL Server and the API**
   ```powershell
   docker compose up --build
   ```
   The API runs at http://localhost:8080. Swagger UI is at http://localhost:8080/swagger.

3. **Start the front end** (in a second terminal)
   ```powershell
   cd web
   npm install
   npm run dev
   ```
   Open http://localhost:5173 and click **Fetch new postings**.

### Running the API outside Docker (for debugging in Visual Studio / VS Code)

Start only the database with `docker compose up db`, then store the connection string as a user secret so it never lands in source control:

```powershell
cd src/JobAssistant.Api
dotnet user-secrets set "ConnectionStrings:JobAssistant" "Server=localhost,1433;Database=JobAssistant;User Id=sa;Password=<your SA_PASSWORD>;TrustServerCertificate=True"
dotnet run --urls http://localhost:8080
```

## Tests

```powershell
dotnet test tests/JobAssistant.Tests
```

Covers job-board JSON parsing, HTML-to-text cleanup, and ingestion behavior (no duplicates on re-run, `LastSeenAt` updates, failing companies isolated, inactive companies skipped).

## API

| Method | Route | Description |
|---|---|---|
| GET | `/api/jobs?search=&companyId=` | Up to 200 postings, newest first |
| GET | `/api/jobs/{id}` | One posting with its full description |
| GET | `/api/companies` | Company watchlist |
| POST | `/api/companies` | Add a company: `{ "name": "Acme", "board": "Greenhouse", "boardToken": "acme" }` |
| POST | `/api/ingest` | Fetch new postings for all active companies |

**Finding a board token:** it's the slug in the company's careers URL: `boards.greenhouse.io/<token>` or `jobs.lever.co/<token>`.
