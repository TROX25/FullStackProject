using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MojProjekt.Application.Crawling;

namespace MojProjekt.Infrastructure.Crawling;

/// <summary>
/// Implements the required "real crawler with a documented fallback" behavior: tries the live OLX
/// crawler first (unless SampleOnly forces the fallback for a deterministic demo), and falls back to
/// the bundled sample dataset if the live crawl is unavailable. CrawlResult.SourceUsed always reports
/// which path was actually taken, and the API/UI surface it transparently.
/// </summary>
public class CompositeListingCrawler(
    [FromKeyedServices("live")] IListingCrawler liveCrawler,
    [FromKeyedServices("sample")] IListingCrawler sampleCrawler,
    IOptions<CrawlerOptions> options,
    ILogger<CompositeListingCrawler> logger) : IListingCrawler
{
    public async Task<CrawlResult> CrawlRecentListingsAsync(CrawlOptions crawlOptions, CancellationToken cancellationToken)
    {
        var mode = options.Value.Mode;

        if (mode == CrawlerMode.SampleOnly)
        {
            logger.LogInformation("Crawler mode is SampleOnly; using bundled sample dataset.");
            return await sampleCrawler.CrawlRecentListingsAsync(crawlOptions, cancellationToken);
        }

        try
        {
            var result = await liveCrawler.CrawlRecentListingsAsync(crawlOptions, cancellationToken);
            if (result.Listings.Count > 0)
            {
                return result;
            }

            logger.LogWarning("Live OLX crawler returned zero listings.");
            if (mode == CrawlerMode.LiveOnly)
            {
                return result;
            }
        }
        catch (CrawlUnavailableException ex)
        {
            logger.LogWarning(ex, "Live OLX crawler unavailable.");
            if (mode == CrawlerMode.LiveOnly)
            {
                throw;
            }
        }

        logger.LogInformation("Falling back to bundled sample dataset.");
        return await sampleCrawler.CrawlRecentListingsAsync(crawlOptions, cancellationToken);
    }
}
