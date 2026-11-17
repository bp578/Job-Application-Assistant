using System.Net;
using System.Text;
using JobAssistant.Api.Ingestion;
using JobAssistant.Api.Models;

namespace JobAssistant.Tests;

/// <summary>Returns a canned HTTP response and records the request URL.</summary>
internal sealed class StubHttpMessageHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
    : HttpMessageHandler
{
    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}

/// <summary>In-memory job board: set postings or failures per board token.</summary>
internal sealed class FakeJobBoardClient(JobBoard board) : IJobBoardClient
{
    public JobBoard Board { get; } = board;

    public Dictionary<string, List<ExternalPosting>> PostingsByToken { get; } = new();

    public HashSet<string> FailingTokens { get; } = new();

    public Task<IReadOnlyList<ExternalPosting>> GetPostingsAsync(string boardToken, CancellationToken ct = default)
    {
        if (FailingTokens.Contains(boardToken))
        {
            throw new HttpRequestException($"Board '{boardToken}' not found");
        }

        IReadOnlyList<ExternalPosting> postings =
            PostingsByToken.GetValueOrDefault(boardToken) ?? new List<ExternalPosting>();
        return Task.FromResult(postings);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
