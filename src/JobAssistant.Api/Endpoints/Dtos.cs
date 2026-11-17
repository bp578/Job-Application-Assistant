using JobAssistant.Api.Models;

namespace JobAssistant.Api.Endpoints;

public record JobSummaryDto(
    int Id,
    string Title,
    string Company,
    string? Location,
    string Url,
    DateTimeOffset? PostedAt,
    DateTimeOffset FirstSeenAt);

public record JobDetailDto(
    int Id,
    string Title,
    string Company,
    string? Location,
    string Url,
    string? DescriptionText,
    DateTimeOffset? PostedAt,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt);

public record CompanyDto(
    int Id,
    string Name,
    JobBoard Board,
    string BoardToken,
    bool IsActive,
    int PostingCount);

public record CreateCompanyRequest(string Name, JobBoard Board, string BoardToken);
