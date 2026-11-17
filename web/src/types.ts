// Mirrors the API's DTOs (camelCase JSON).

export interface JobSummary {
  id: number;
  title: string;
  company: string;
  location: string | null;
  url: string;
  postedAt: string | null;
  firstSeenAt: string;
}

export interface CompanyIngestionResult {
  companyId: number;
  companyName: string;
  success: boolean;
  fetched: number;
  added: number;
  updated: number;
  error: string | null;
}

export interface IngestionResult {
  companies: CompanyIngestionResult[];
  totalAdded: number;
  failedCompanies: number;
}
