using MediatR;
using Microsoft.AspNetCore.Mvc;
using MojProjekt.Application.Crawling.Commands;
using MojProjekt.Application.Crawling.Queries;

namespace MojProjekt.Api.Controllers;

[ApiController]
[Route("api/crawl")]
public class CrawlController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> TriggerCrawl()
    {
        var crawlRunId = await sender.Send(new TriggerCrawlCommand());
        return AcceptedAtAction(nameof(GetCrawlRun), new { id = crawlRunId }, new { crawlRunId, status = "Running" });
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestCrawlRun()
    {
        var run = await sender.Send(new GetLatestCrawlRunQuery());
        return run is null ? NotFound() : Ok(run);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCrawlRun(Guid id)
    {
        var run = await sender.Send(new GetCrawlRunQuery(id));
        return run is null ? NotFound() : Ok(run);
    }
}
