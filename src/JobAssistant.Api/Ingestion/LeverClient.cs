using System.Text.Json.Serialization;
using JobAssistant.Api.Models;

namespace JobAssistant.Api.Ingestion;

/// <summary>
/// Reads postings from Lever's public Postings API:
/// GET https://api.lever.co/v0/postings/{token}?mode=json
/// </summary>
public class LeverClient(HttpClient http) : IJobBoardClient
{
    public JobBoard Board => JobBoard.Lever;

    public async Task<IReadOnlyList<ExternalPosting>> GetPostingsAsync(string boardToken, CancellationToken ct = default)
    {
        var postings = await http.GetFromJsonAsync<List<LeverPosting>>(
            $"v0/postings/{Uri.EscapeDataString(boardToken)}?mode=json", ct)
            ?? new List<LeverPosting>();

        return postings
            .Select(posting => new ExternalPosting(
                ExternalId: posting.Id,
                Title: posting.Text,
                Location: posting.Categories?.Location,
                Url: posting.HostedUrl,
                DescriptionText: string.IsNullOrWhiteSpace(posting.DescriptionPlain)
                    ? null
                    : posting.DescriptionPlain.Trim(),
                PostedAt: posting.CreatedAt is long ms
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ms)
                    : null))
            .ToList();
    }

    private sealed record LeverPosting(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("hostedUrl")] string HostedUrl,
        [property: JsonPropertyName("categories")] LeverCategories? Categories,
        [property: JsonPropertyName("createdAt")] long? CreatedAt,
        [property: JsonPropertyName("descriptionPlain")] string? DescriptionPlain);

    private sealed record LeverCategories(
        [property: JsonPropertyName("location")] string? Location);
}
