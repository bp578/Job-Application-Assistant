using JobAssistant.Api.Models;

namespace JobAssistant.Api.Ingestion;

/// <summary>A posting as returned by a job board, before it's saved.</summary>
public record ExternalPosting(
    string ExternalId,
    string Title,
    string? Location,
    string Url,
    string? DescriptionText,
    DateTimeOffset? PostedAt);

/// <summary>Fetches open postings from one job board's public API.</summary>
public interface IJobBoardClient
{
    JobBoard Board { get; }

    Task<IReadOnlyList<ExternalPosting>> GetPostingsAsync(string boardToken, CancellationToken ct = default);
}
