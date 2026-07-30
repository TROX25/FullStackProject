using MediatR;
using MojProjekt.Application.Crawling.Contracts;
using MojProjekt.Application.Crawling.Mapping;

namespace MojProjekt.Application.Crawling.Queries;

public sealed class GetLatestCrawlRunQueryHandler(ICrawlRunRepository repository)
    : IRequestHandler<GetLatestCrawlRunQuery, CrawlRunDto?>
{
    public async Task<CrawlRunDto?> Handle(GetLatestCrawlRunQuery request, CancellationToken cancellationToken)
    {
        var run = await repository.GetLatestAsync(cancellationToken);
        return run?.ToDto();
    }
}
