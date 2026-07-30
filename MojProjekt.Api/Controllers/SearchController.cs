using MediatR;
using Microsoft.AspNetCore.Mvc;
using MojProjekt.Application.Search.Contracts;
using MojProjekt.Application.Search.Queries;

namespace MojProjekt.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(ISender sender) : ControllerBase
{
    private const int DefaultMaxResults = 20;
    private const int HardMaxResults = 100;

    [HttpPost]
    public async Task<IActionResult> Search([FromBody] SearchRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query must not be empty." });
        }

        var maxResults = request.MaxResults is > 0 and <= HardMaxResults
            ? request.MaxResults.Value
            : DefaultMaxResults;

        var response = await sender.Send(new SearchListingsQuery(request.Query, maxResults));
        return Ok(response);
    }
}
