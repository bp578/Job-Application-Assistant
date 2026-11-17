using JobAssistant.Api.Data;
using JobAssistant.Api.Ingestion;
using JobAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobAssistant.Tests;

public class IngestionServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly FakeJobBoardClient _greenhouse = new(JobBoard.Greenhouse);
    private readonly FakeJobBoardClient _lever = new(JobBoard.Lever);
    private readonly FixedTimeProvider _time = new(Start);

    private static ExternalPosting Posting(string id, string title = "Software Engineer") =>
        new(id, title, "Philadelphia, PA", $"https://example.com/jobs/{id}", "Description", null);

    private void AddCompanies(params Company[] companies)
    {
        using var db = new AppDbContext(_options);
        db.Companies.AddRange(companies);
        db.SaveChanges();
    }

    // A fresh DbContext per run, like one HTTP request in the real app.
    private async Task<IngestionResult> RunAsync()
    {
        await using var db = new AppDbContext(_options);
        var service = new IngestionService(
            db, [_greenhouse, _lever], _time, NullLogger<IngestionService>.Instance);
        return await service.RunAsync();
    }

    private List<JobPosting> AllPostings()
    {
        using var db = new AppDbContext(_options);
        return db.JobPostings.OrderBy(p => p.ExternalId).ToList();
    }

    [Fact]
    public async Task FirstRun_AddsAllPostings()
    {
        AddCompanies(new Company { Name = "Acme", Board = JobBoard.Greenhouse, BoardToken = "acme" });
        _greenhouse.PostingsByToken["acme"] = [Posting("1"), Posting("2")];

        var result = await RunAsync();

        Assert.Equal(2, result.TotalAdded);
        var postings = AllPostings();
        Assert.Equal(2, postings.Count);
        Assert.All(postings, p => Assert.Equal(Start, p.FirstSeenAt));
    }

    [Fact]
    public async Task SecondRun_DoesNotDuplicate_AndUpdatesLastSeen()
    {
        AddCompanies(new Company { Name = "Acme", Board = JobBoard.Greenhouse, BoardToken = "acme" });
        _greenhouse.PostingsByToken["acme"] = [Posting("1"), Posting("2")];
        await RunAsync();

        var later = Start.AddHours(6);
        _time.Now = later;
        _greenhouse.PostingsByToken["acme"] = [Posting("1", "Software Engineer II"), Posting("2")];

        var result = await RunAsync();

        var acme = Assert.Single(result.Companies);
        Assert.Equal(0, acme.Added);
        Assert.Equal(2, acme.Updated);

        var postings = AllPostings();
        Assert.Equal(2, postings.Count);
        Assert.Equal("Software Engineer II", postings[0].Title);
        Assert.All(postings, p =>
        {
            Assert.Equal(Start, p.FirstSeenAt);
            Assert.Equal(later, p.LastSeenAt);
        });
    }

    [Fact]
    public async Task DuplicateIdsInOneResponse_AreSavedOnce()
    {
        AddCompanies(new Company { Name = "Acme", Board = JobBoard.Greenhouse, BoardToken = "acme" });
        _greenhouse.PostingsByToken["acme"] = [Posting("1"), Posting("1")];

        var result = await RunAsync();

        Assert.Equal(1, result.TotalAdded);
        Assert.Single(AllPostings());
    }

    [Fact]
    public async Task FailingCompany_IsReported_AndOthersStillRun()
    {
        AddCompanies(
            new Company { Name = "Broken Co", Board = JobBoard.Greenhouse, BoardToken = "broken" },
            new Company { Name = "Lever Co", Board = JobBoard.Lever, BoardToken = "leverco" });
        _greenhouse.FailingTokens.Add("broken");
        _lever.PostingsByToken["leverco"] = [Posting("abc")];

        var result = await RunAsync();

        Assert.Equal(1, result.FailedCompanies);
        var broken = result.Companies.Single(c => c.CompanyName == "Broken Co");
        Assert.False(broken.Success);
        Assert.Contains("not found", broken.Error);

        var leverCo = result.Companies.Single(c => c.CompanyName == "Lever Co");
        Assert.True(leverCo.Success);
        Assert.Equal(1, leverCo.Added);
    }

    [Fact]
    public async Task InactiveCompanies_AreSkipped()
    {
        AddCompanies(new Company { Name = "Paused", Board = JobBoard.Greenhouse, BoardToken = "paused", IsActive = false });
        _greenhouse.PostingsByToken["paused"] = [Posting("1")];

        var result = await RunAsync();

        Assert.Empty(result.Companies);
        Assert.Empty(AllPostings());
    }

    [Fact]
    public async Task LongTitles_AreTruncatedToColumnLength()
    {
        AddCompanies(new Company { Name = "Acme", Board = JobBoard.Greenhouse, BoardToken = "acme" });
        _greenhouse.PostingsByToken["acme"] = [Posting("1", new string('x', 500))];

        await RunAsync();

        Assert.Equal(JobPosting.TitleMaxLength, AllPostings()[0].Title.Length);
    }
}
