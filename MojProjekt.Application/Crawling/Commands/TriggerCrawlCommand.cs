using MediatR;

namespace MojProjekt.Application.Crawling.Commands;

/// <summary>
/// Creates a Pending CrawlRun and returns immediately with its id; the actual crawl work is
/// executed out-of-band by ICrawlRunner so the HTTP request isn't held open for the crawl duration.
/// </summary>
public sealed record TriggerCrawlCommand : IRequest<Guid>;
