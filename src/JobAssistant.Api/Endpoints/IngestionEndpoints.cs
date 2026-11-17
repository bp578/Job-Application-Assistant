using JobAssistant.Api.Ingestion;

namespace JobAssistant.Api.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        // Phase 1: triggered manually. Phase 2 moves this to a scheduled background worker and queue.
        app.MapPost("/api/ingest", async (IngestionService ingestion, CancellationToken ct) =>
            TypedResults.Ok(await ingestion.RunAsync(ct)))
            .WithTags("Ingestion")
            .WithSummary("Fetch new postings for every active company on the watchlist.");

        return app;
    }
}
