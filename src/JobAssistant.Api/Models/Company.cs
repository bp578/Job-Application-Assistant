namespace JobAssistant.Api.Models;

public enum JobBoard
{
    Greenhouse,
    Lever
}

/// <summary>A company on the watchlist, identified by its public job-board token.</summary>
public class Company
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public JobBoard Board { get; set; }

    /// <summary>
    /// The company's slug on its job board, e.g. "anthropic" in
    /// https://boards.greenhouse.io/anthropic or "palantir" in https://jobs.lever.co/palantir.
    /// </summary>
    public required string BoardToken { get; set; }

    public bool IsActive { get; set; } = true;

    public List<JobPosting> JobPostings { get; set; } = [];
}
