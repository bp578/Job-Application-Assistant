using JobAssistant.Api.Data;
using JobAssistant.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace JobAssistant.Api.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies").WithTags("Companies");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var companies = await db.Companies
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CompanyDto(
                    c.Id, c.Name, c.Board, c.BoardToken, c.IsActive, c.JobPostings.Count))
                .ToListAsync(ct);

            return TypedResults.Ok(companies);
        })
        .WithSummary("List the company watchlist.");

        group.MapPost("/", async Task<Results<Created<CompanyDto>, ValidationProblem, Conflict<string>>> (
            CreateCompanyRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors["name"] = ["Name is required."];
            }
            if (string.IsNullOrWhiteSpace(request.BoardToken))
            {
                errors["boardToken"] = ["Board token is required, e.g. \"anthropic\" from boards.greenhouse.io/anthropic."];
            }
            if (!Enum.IsDefined(request.Board))
            {
                errors["board"] = ["Board must be Greenhouse or Lever."];
            }
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var token = request.BoardToken.Trim().ToLowerInvariant();
            var alreadyExists = await db.Companies
                .AnyAsync(c => c.Board == request.Board && c.BoardToken == token, ct);
            if (alreadyExists)
            {
                return TypedResults.Conflict($"{request.Board} board \"{token}\" is already on the watchlist.");
            }

            var company = new Company
            {
                Name = request.Name.Trim(),
                Board = request.Board,
                BoardToken = token
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync(ct);

            var dto = new CompanyDto(company.Id, company.Name, company.Board, company.BoardToken, company.IsActive, 0);
            return TypedResults.Created($"/api/companies/{company.Id}", dto);
        })
        .WithSummary("Add a company to the watchlist.");

        return app;
    }
}
