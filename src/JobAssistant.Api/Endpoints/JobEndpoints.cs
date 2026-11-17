using JobAssistant.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace JobAssistant.Api.Endpoints;

public static class JobEndpoints
{
    private const int MaxResults = 200;

    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jobs").WithTags("Jobs");

        group.MapGet("/", async (string? search, int? companyId, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.JobPostings.AsNoTracking();

            if (companyId is not null)
            {
                query = query.Where(p => p.CompanyId == companyId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p =>
                    p.Title.Contains(term)
                    || (p.Location != null && p.Location.Contains(term))
                    || p.Company!.Name.Contains(term));
            }

            var jobs = await query
                .OrderByDescending(p => p.FirstSeenAt)
                .ThenBy(p => p.Title)
                .Take(MaxResults)
                .Select(p => new JobSummaryDto(
                    p.Id, p.Title, p.Company!.Name, p.Location, p.Url, p.PostedAt, p.FirstSeenAt))
                .ToListAsync(ct);

            return TypedResults.Ok(jobs);
        })
        .WithSummary($"List up to {MaxResults} postings, newest first. Optional search by title, location, or company.");

        group.MapGet("/{id:int}", async Task<Results<Ok<JobDetailDto>, NotFound>> (
            int id, AppDbContext db, CancellationToken ct) =>
        {
            var job = await db.JobPostings
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new JobDetailDto(
                    p.Id, p.Title, p.Company!.Name, p.Location, p.Url, p.DescriptionText,
                    p.PostedAt, p.FirstSeenAt, p.LastSeenAt))
                .FirstOrDefaultAsync(ct);

            return job is null ? TypedResults.NotFound() : TypedResults.Ok(job);
        })
        .WithSummary("Get one posting, including its plain-text description.");

        return app;
    }
}
