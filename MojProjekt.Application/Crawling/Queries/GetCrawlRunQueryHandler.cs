using MediatR;
using MojProjekt.Application.Crawling.Contracts;
using MojProjekt.Application.Crawling.Mapping;

namespace MojProjekt.Application.Crawling.Queries;

public sealed class GetCrawlRunQueryHandler(ICrawlRunRepository repository)
    : IRequestHandler<GetCrawlRunQuery, CrawlRunDto?>
{
    public async Task<CrawlRunDto?> Handle(GetCrawlRunQuery request, CancellationToken cancellationToken)
    {
        var run = await repository.GetByIdAsync(request.Id, cancellationToken);
        return run?.ToDto();
    }
}
