import type { IngestionResult, JobSummary } from "./types";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, init);
  if (!response.ok) {
    throw new Error(`${init?.method ?? "GET"} ${path} failed: ${response.status} ${response.statusText}`);
  }
  return (await response.json()) as T;
}

export function getJobs(search: string): Promise<JobSummary[]> {
  const params = new URLSearchParams();
  const term = search.trim();
  if (term) params.set("search", term);
  const query = params.toString();
  return request<JobSummary[]>(`/api/jobs${query ? `?${query}` : ""}`);
}

export function runIngestion(): Promise<IngestionResult> {
  return request<IngestionResult>("/api/ingest", { method: "POST" });
}
