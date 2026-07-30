using MediatR;
using Microsoft.AspNetCore.Mvc;
using MojProjekt.Application.Listings.Queries;

namespace MojProjekt.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetListings([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await sender.Send(new GetListingsQuery(page, pageSize)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetListingById(Guid id)
    {
        var listing = await sender.Send(new GetListingByIdQuery(id));
        return listing is null ? NotFound() : Ok(listing);
    }
}
