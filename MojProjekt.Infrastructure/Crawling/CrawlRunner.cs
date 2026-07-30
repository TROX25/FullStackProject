using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MojProjekt.Application.Crawling;
using MojProjekt.Domain.Crawling;

namespace MojProjekt.Infrastructure.Crawling;

/// <summary>
/// Runs a previously-created CrawlRun on a background Task using its own DI scope, so the HTTP
/// request that triggered it (POST /api/crawl) can return immediately. The frontend polls
/// GET /api/crawl/{id} for progress instead of holding the connection open for the crawl duration.
/// </summary>
public class CrawlRunner(IServiceScopeFactory scopeFactory, ILogger<CrawlRunner> logger) : ICrawlRunner
{
    private static readonly CrawlOptions DefaultOptions = new(MaxAge: TimeSpan.FromHours(24), MaxListings: 100);

    public void RunInBackground(Guid crawlRunId)
    {
        _ = Task.Run(() => ExecuteAsync(crawlRunId));
    }

    private async Task ExecuteAsync(Guid crawlRunId)
    {
        using var scope = scopeFactory.CreateScope();
        var crawlRunRepository = scope.ServiceProvider.GetRequiredService<ICrawlRunRepository>();
        var listingRepository = scope.ServiceProvider.GetRequiredService<MojProjekt.Application.Listings.IListingRepository>();
        var crawler = scope.ServiceProvider.GetRequiredService<IListingCrawler>();

        var run = await crawlRunRepository.GetByIdAsync(crawlRunId, CancellationToken.None);
        if (run is null)
        {
            logger.LogError("CrawlRun {CrawlRunId} not found; cannot execute.", crawlRunId);
            return;
        }

        run.Status = CrawlStatus.Running;
        await crawlRunRepository.UpdateAsync(run, CancellationToken.None);

        try
        {
            var result = await crawler.CrawlRecentListingsAsync(DefaultOptions, CancellationToken.None);
            await listingRepository.UpsertRangeAsync(result.Listings, CancellationToken.None);

            run.Status = CrawlStatus.Completed;
            run.SourceUsed = result.SourceUsed;
            run.ListingsFound = result.Listings.Count;
            run.CompletedAt = DateTimeOffset.UtcNow;

            await crawlRunRepository.UpdateAsync(run, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Crawl run {CrawlRunId} failed.", crawlRunId);

            // A failure inside UpsertRangeAsync can leave this scope's DbContext/ChangeTracker in a
            // state where a further SaveChanges also throws; write the Failed status through a fresh
            // scope so a mid-crawl error can never leave a run stuck at "Running" forever.
            using var failureScope = scopeFactory.CreateScope();
            var failureRepository = failureScope.ServiceProvider.GetRequiredService<ICrawlRunRepository>();
            var failedRun = await failureRepository.GetByIdAsync(crawlRunId, CancellationToken.None);
            if (failedRun is not null)
            {
                failedRun.Status = CrawlStatus.Failed;
                failedRun.ErrorMessage = ex.Message;
                failedRun.CompletedAt = DateTimeOffset.UtcNow;
                await failureRepository.UpdateAsync(failedRun, CancellationToken.None);
            }
        }
    }
}
