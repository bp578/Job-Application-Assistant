using JobAssistant.Api.Ingestion;

namespace JobAssistant.Tests;

public class GreenhouseClientTests
{
    private const string SampleJson = """
        {
          "jobs": [
            {
              "id": 4012345,
              "title": "Software Engineer, Platform",
              "absolute_url": "https://boards.greenhouse.io/example/jobs/4012345",
              "location": { "name": "Remote - US" },
              "updated_at": "2026-11-10T09:30:00-05:00",
              "content": "&lt;p&gt;Build &amp;amp; ship&lt;/p&gt;"
            },
            {
              "id": 4012346,
              "title": "Data Engineer",
              "absolute_url": "https://boards.greenhouse.io/example/jobs/4012346",
              "location": null,
              "updated_at": null,
              "content": null
            }
          ]
        }
        """;

    private static (GreenhouseClient Client, StubHttpMessageHandler Handler) Create(string body)
    {
        var handler = new StubHttpMessageHandler(body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://boards-api.greenhouse.io/") };
        return (new GreenhouseClient(http), handler);
    }

    [Fact]
    public async Task GetPostingsAsync_MapsFields()
    {
        var (client, _) = Create(SampleJson);

        var postings = await client.GetPostingsAsync("example");

        Assert.Equal(2, postings.Count);
        var first = postings[0];
        Assert.Equal("4012345", first.ExternalId);
        Assert.Equal("Software Engineer, Platform", first.Title);
        Assert.Equal("Remote - US", first.Location);
        Assert.Equal("https://boards.greenhouse.io/example/jobs/4012345", first.Url);
        Assert.Equal("Build & ship", first.DescriptionText);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 9, 30, 0, TimeSpan.FromHours(-5)), first.PostedAt);

        var second = postings[1];
        Assert.Null(second.Location);
        Assert.Null(second.DescriptionText);
        Assert.Null(second.PostedAt);
    }

    [Fact]
    public async Task GetPostingsAsync_CallsBoardEndpointWithContent()
    {
        var (client, handler) = Create("""{ "jobs": [] }""");

        var postings = await client.GetPostingsAsync("example");

        Assert.Empty(postings);
        Assert.Equal("https://boards-api.greenhouse.io/v1/boards/example/jobs?content=true",
            handler.LastRequestUri?.ToString());
    }
}

public class LeverClientTests
{
    private const string SampleJson = """
        [
          {
            "id": "a1b2c3d4-0000-4000-8000-000000000001",
            "text": "Backend Engineer",
            "hostedUrl": "https://jobs.lever.co/example/a1b2c3d4-0000-4000-8000-000000000001",
            "categories": { "location": "New York, NY", "team": "Engineering", "commitment": "Full-time" },
            "createdAt": 1762790400000,
            "descriptionPlain": "  Design and build services.  "
          }
        ]
        """;

    [Fact]
    public async Task GetPostingsAsync_MapsFields()
    {
        var handler = new StubHttpMessageHandler(SampleJson);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.lever.co/") };
        var client = new LeverClient(http);

        var postings = await client.GetPostingsAsync("example");

        var posting = Assert.Single(postings);
        Assert.Equal("a1b2c3d4-0000-4000-8000-000000000001", posting.ExternalId);
        Assert.Equal("Backend Engineer", posting.Title);
        Assert.Equal("New York, NY", posting.Location);
        Assert.Equal("Design and build services.", posting.DescriptionText);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1762790400000), posting.PostedAt);
        Assert.Equal("https://api.lever.co/v0/postings/example?mode=json", handler.LastRequestUri?.ToString());
    }
}
