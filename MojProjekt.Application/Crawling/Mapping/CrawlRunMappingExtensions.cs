using MojProjekt.Application.Crawling.Contracts;
using MojProjekt.Domain.Crawling;

namespace MojProjekt.Application.Crawling.Mapping;

public static class CrawlRunMappingExtensions
{
    public static CrawlRunDto ToDto(this CrawlRun run) => new(
        run.Id,
        run.Status.ToString(),
        run.SourceUsed.ToString(),
        run.ListingsFound,
        run.StartedAt,
        run.CompletedAt,
        run.ErrorMessage);
}
