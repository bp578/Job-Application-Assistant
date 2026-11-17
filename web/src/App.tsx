import { useCallback, useEffect, useState } from "react";
import { getJobs, runIngestion } from "./api";
import type { IngestionResult, JobSummary } from "./types";
import "./App.css";

const dateFormat = new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" });

function formatDate(value: string | null): string {
  return value ? dateFormat.format(new Date(value)) : "—";
}

function errorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export default function App() {
  const [jobs, setJobs] = useState<JobSummary[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [ingesting, setIngesting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastRun, setLastRun] = useState<IngestionResult | null>(null);

  const loadJobs = useCallback(async (term: string) => {
    setLoading(true);
    setError(null);
    try {
      setJobs(await getJobs(term));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  // Debounce search so typing doesn't send a request per keystroke.
  useEffect(() => {
    const handle = setTimeout(() => void loadJobs(search), 300);
    return () => clearTimeout(handle);
  }, [search, loadJobs]);

  async function handleFetch() {
    setIngesting(true);
    setError(null);
    try {
      setLastRun(await runIngestion());
      await loadJobs(search);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setIngesting(false);
    }
  }

  const failed = lastRun?.companies.filter((c) => !c.success) ?? [];

  return (
    <main className="app">
      <header className="app-header">
        <div>
          <h1>Job Application Assistant</h1>
          <p className="subtitle">Open postings from your company watchlist</p>
        </div>
        <button type="button" onClick={handleFetch} disabled={ingesting}>
          {ingesting ? "Fetching…" : "Fetch new postings"}
        </button>
      </header>

      {lastRun && (
        <div className="run-summary">
          <p>
            Last fetch: {lastRun.totalAdded} new posting{lastRun.totalAdded === 1 ? "" : "s"} across{" "}
            {lastRun.companies.length} compan{lastRun.companies.length === 1 ? "y" : "ies"}.
          </p>
          {failed.length > 0 && (
            <ul className="failures">
              {failed.map((c) => (
                <li key={c.companyId}>
                  {c.companyName}: {c.error}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      <input
        className="search"
        type="search"
        placeholder="Search by title, company, or location"
        aria-label="Search postings"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      {error && <p className="error">{error}</p>}

      {loading ? (
        <p className="muted">Loading…</p>
      ) : jobs.length === 0 ? (
        <p className="muted">
          {search ? "No postings match your search." : "No postings yet. Click “Fetch new postings” to pull from your watchlist."}
        </p>
      ) : (
        <table className="jobs">
          <thead>
            <tr>
              <th>Title</th>
              <th>Company</th>
              <th>Location</th>
              <th>Posted</th>
              <th>First seen</th>
            </tr>
          </thead>
          <tbody>
            {jobs.map((job) => (
              <tr key={job.id}>
                <td>
                  <a href={job.url} target="_blank" rel="noreferrer">
                    {job.title}
                  </a>
                </td>
                <td>{job.company}</td>
                <td>{job.location ?? "—"}</td>
                <td>{formatDate(job.postedAt)}</td>
                <td>{formatDate(job.firstSeenAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
