using MediatR;
using MojProjekt.Application.Listings.Queries;

namespace MojProjekt.Api.Endpoints;

public static class ListingsEndpoints
{
    public static void MapListingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/listings").WithTags("Listings");

        group.MapGet("/", async (ISender sender, int page = 1, int pageSize = 20) =>
                Results.Ok(await sender.Send(new GetListingsQuery(page, pageSize))))
            .WithName("GetListings");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
            {
                var listing = await sender.Send(new GetListingByIdQuery(id));
                return listing is null ? Results.NotFound() : Results.Ok(listing);
            })
            .WithName("GetListingById");
    }
}
