using MojProjekt.Domain.Crawling;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Application.Crawling;

public sealed record CrawlOptions(TimeSpan MaxAge, int MaxListings);

public sealed record CrawlResult(
    IReadOnlyList<Listing> Listings,
    CrawlSourceUsed SourceUsed,
    string? Diagnostics);

/// <summary>Thrown by a crawler implementation when it cannot produce results (blocked, network error, parse failure rate too high).</summary>
public sealed class CrawlUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public interface IListingCrawler
{
    Task<CrawlResult> CrawlRecentListingsAsync(CrawlOptions options, CancellationToken cancellationToken);
}
