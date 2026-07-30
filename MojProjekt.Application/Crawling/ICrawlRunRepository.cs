using MojProjekt.Domain.Crawling;

namespace MojProjekt.Application.Crawling;

public interface ICrawlRunRepository
{
    Task AddAsync(CrawlRun run, CancellationToken cancellationToken);

    Task UpdateAsync(CrawlRun run, CancellationToken cancellationToken);

    Task<CrawlRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CrawlRun?> GetLatestAsync(CancellationToken cancellationToken);
}
