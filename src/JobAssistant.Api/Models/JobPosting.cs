namespace JobAssistant.Api.Models;

public class JobPosting
{
    public const int TitleMaxLength = 300;
    public const int LocationMaxLength = 300;

    public int Id { get; set; }

    public int CompanyId { get; set; }

    public Company? Company { get; set; }

    /// <summary>The posting's ID on the job board. Unique per company; used to skip duplicates.</summary>
    public required string ExternalId { get; set; }

    public required string Title { get; set; }

    public string? Location { get; set; }

    public required string Url { get; set; }

    /// <summary>Plain-text job description (HTML stripped). Used for AI screening in Phase 2.</summary>
    public string? DescriptionText { get; set; }

    /// <summary>When the board says the posting was published or last updated.</summary>
    public DateTimeOffset? PostedAt { get; set; }

    /// <summary>When this app first saw the posting.</summary>
    public DateTimeOffset FirstSeenAt { get; set; }

    /// <summary>When this app last saw the posting on the board. Old values suggest it was taken down.</summary>
    public DateTimeOffset LastSeenAt { get; set; }
}
