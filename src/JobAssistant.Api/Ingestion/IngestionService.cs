using System.Text.Json;
using JobAssistant.Api.Data;
using JobAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobAssistant.Api.Ingestion;

public record CompanyIngestionResult(
    int CompanyId,
    string CompanyName,
    bool Success,
    int Fetched,
    int Added,
    int Updated,
    string? Error)
{
    public static CompanyIngestionResult Succeeded(Company company, int fetched, int added, int updated) =>
        new(company.Id, company.Name, true, fetched, added, updated, null);

    public static CompanyIngestionResult Failed(Company company, string error) =>
        new(company.Id, company.Name, false, 0, 0, 0, error);
}

public record IngestionResult(IReadOnlyList<CompanyIngestionResult> Companies)
{
    public int TotalAdded => Companies.Sum(c => c.Added);

    public int FailedCompanies => Companies.Count(c => !c.Success);
}

/// <summary>
/// Pulls postings for every active company on the watchlist and upserts them.
/// One company failing (bad token, board down) is recorded and doesn't stop the rest.
/// </summary>
public class IngestionService(
    AppDbContext db,
    IEnumerable<IJobBoardClient> clients,
    TimeProvider time,
    ILogger<IngestionService> logger)
{
    public async Task<IngestionResult> RunAsync(CancellationToken ct = default)
    {
        var clientsByBoard = clients.ToDictionary(c => c.Board);

        var companies = await db.Companies
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var results = new List<CompanyIngestionResult>();
        foreach (var company in companies)
        {
            results.Add(await IngestCompanyAsync(company, clientsByBoard, ct));
        }

        return new IngestionResult(results);
    }

    private async Task<CompanyIngestionResult> IngestCompanyAsync(
        Company company,
        Dictionary<JobBoard, IJobBoardClient> clientsByBoard,
        CancellationToken ct)
    {
        if (!clientsByBoard.TryGetValue(company.Board, out var client))
        {
            return CompanyIngestionResult.Failed(company, $"No client is registered for {company.Board}.");
        }

        IReadOnlyList<ExternalPosting> fetched;
        try
        {
            fetched = await client.GetPostingsAsync(company.BoardToken, ct);
        }
        catch (Exception ex) when (
            (ex is HttpRequestException or JsonException or TaskCanceledException)
            && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Fetching postings for {Company} ({Board}/{Token}) failed",
                company.Name, company.Board, company.BoardToken);
            return CompanyIngestionResult.Failed(company, ex.Message);
        }

        var now = time.GetUtcNow();
        var existing = await db.JobPostings
            .Where(p => p.CompanyId == company.Id)
            .ToDictionaryAsync(p => p.ExternalId, ct);

        int added = 0, updated = 0;
        // Skip malformed postings (no ID or link) and duplicates within one response.
        var valid = fetched
            .Where(p => !string.IsNullOrWhiteSpace(p.ExternalId) && !string.IsNullOrWhiteSpace(p.Url))
            .DistinctBy(p => p.ExternalId);

        foreach (var posting in valid)
        {
            var title = Truncate(posting.Title, JobPosting.TitleMaxLength) ?? "(untitled)";
            var location = Truncate(posting.Location, JobPosting.LocationMaxLength);

            if (existing.TryGetValue(posting.ExternalId, out var row))
            {
                row.Title = title;
                row.Location = location;
                row.Url = posting.Url;
                row.DescriptionText = posting.DescriptionText;
                row.PostedAt = posting.PostedAt;
                row.LastSeenAt = now;
                updated++;
            }
            else
            {
                db.JobPostings.Add(new JobPosting
                {
                    CompanyId = company.Id,
                    ExternalId = posting.ExternalId,
                    Title = title,
                    Location = location,
                    Url = posting.Url,
                    DescriptionText = posting.DescriptionText,
                    PostedAt = posting.PostedAt,
                    FirstSeenAt = now,
                    LastSeenAt = now
                });
                added++;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("{Company}: fetched {Fetched}, added {Added}, updated {Updated}",
            company.Name, fetched.Count, added, updated);

        return CompanyIngestionResult.Succeeded(company, fetched.Count, added, updated);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
