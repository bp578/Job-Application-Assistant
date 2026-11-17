using System.Globalization;
using System.Text.Json.Serialization;
using JobAssistant.Api.Models;

namespace JobAssistant.Api.Ingestion;

/// <summary>
/// Reads postings from Greenhouse's public Job Board API:
/// GET https://boards-api.greenhouse.io/v1/boards/{token}/jobs?content=true
/// </summary>
public class GreenhouseClient(HttpClient http) : IJobBoardClient
{
    public JobBoard Board => JobBoard.Greenhouse;

    public async Task<IReadOnlyList<ExternalPosting>> GetPostingsAsync(string boardToken, CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<GreenhouseJobsResponse>(
            $"v1/boards/{Uri.EscapeDataString(boardToken)}/jobs?content=true", ct);

        var jobs = response?.Jobs ?? new List<GreenhouseJob>();

        return jobs
            .Select(job => new ExternalPosting(
                ExternalId: job.Id.ToString(CultureInfo.InvariantCulture),
                Title: job.Title,
                Location: job.Location?.Name,
                Url: job.AbsoluteUrl,
                DescriptionText: HtmlText.ToPlainText(job.Content),
                PostedAt: job.UpdatedAt))
            .ToList();
    }

    private sealed record GreenhouseJobsResponse(
        [property: JsonPropertyName("jobs")] List<GreenhouseJob>? Jobs);

    private sealed record GreenhouseJob(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("absolute_url")] string AbsoluteUrl,
        [property: JsonPropertyName("location")] GreenhouseLocation? Location,
        [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt,
        [property: JsonPropertyName("content")] string? Content);

    private sealed record GreenhouseLocation(
        [property: JsonPropertyName("name")] string? Name);
}
