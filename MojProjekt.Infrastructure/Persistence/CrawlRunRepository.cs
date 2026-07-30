using Microsoft.EntityFrameworkCore;
using MojProjekt.Application.Crawling;
using MojProjekt.Domain.Crawling;

namespace MojProjekt.Infrastructure.Persistence;

public class CrawlRunRepository(AppDbContext dbContext) : ICrawlRunRepository
{
    public async Task AddAsync(CrawlRun run, CancellationToken cancellationToken)
    {
        dbContext.CrawlRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CrawlRun run, CancellationToken cancellationToken)
    {
        dbContext.CrawlRuns.Update(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<CrawlRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.CrawlRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<CrawlRun?> GetLatestAsync(CancellationToken cancellationToken) =>
        dbContext.CrawlRuns.OrderByDescending(r => r.StartedAt).FirstOrDefaultAsync(cancellationToken);
}
