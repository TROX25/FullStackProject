using MediatR;
using MojProjekt.Application.Search.Contracts;
using MojProjekt.Application.Search.Queries;

namespace MojProjekt.Api.Endpoints;

public static class SearchEndpoints
{
    private const int DefaultMaxResults = 20;
    private const int HardMaxResults = 100;

    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search").WithTags("Search");

        group.MapPost("/", async (SearchRequestDto request, ISender sender) =>
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query must not be empty." });
                }

                var maxResults = request.MaxResults is > 0 and <= HardMaxResults
                    ? request.MaxResults.Value
                    : DefaultMaxResults;

                var response = await sender.Send(new SearchListingsQuery(request.Query, maxResults));
                return Results.Ok(response);
            })
            .WithName("SearchListings");
    }
}
