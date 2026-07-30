using MediatR;
using MojProjekt.Domain.Crawling;

namespace MojProjekt.Application.Crawling.Commands;

public sealed class TriggerCrawlCommandHandler(ICrawlRunRepository crawlRunRepository, ICrawlRunner crawlRunner)
    : IRequestHandler<TriggerCrawlCommand, Guid>
{
    public async Task<Guid> Handle(TriggerCrawlCommand request, CancellationToken cancellationToken)
    {
        var run = new CrawlRun();
        await crawlRunRepository.AddAsync(run, cancellationToken);

        // Fire-and-forget on a background execution path; the caller polls GET /api/crawl/{id} for progress.
        crawlRunner.RunInBackground(run.Id);

        return run.Id;
    }
}
