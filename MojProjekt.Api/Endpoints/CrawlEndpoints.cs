using MediatR;
using MojProjekt.Application.Crawling.Commands;
using MojProjekt.Application.Crawling.Queries;

namespace MojProjekt.Api.Endpoints;

public static class CrawlEndpoints
{
    public static void MapCrawlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crawl").WithTags("Crawl");

        group.MapPost("/", async (ISender sender) =>
            {
                var crawlRunId = await sender.Send(new TriggerCrawlCommand());
                return Results.Accepted($"/api/crawl/{crawlRunId}", new { crawlRunId, status = "Running" });
            })
            .WithName("TriggerCrawl");

        group.MapGet("/latest", async (ISender sender) =>
            {
                var run = await sender.Send(new GetLatestCrawlRunQuery());
                return run is null ? Results.NotFound() : Results.Ok(run);
            })
            .WithName("GetLatestCrawlRun");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
            {
                var run = await sender.Send(new GetCrawlRunQuery(id));
                return run is null ? Results.NotFound() : Results.Ok(run);
            })
            .WithName("GetCrawlRun");
    }
}
