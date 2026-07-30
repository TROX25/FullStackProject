using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MojProjekt.Application.Crawling;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Crawling.Olx;

/// <summary>
/// Live crawler for OLX.pl's "Motoryzacja / Samochody" section. Respects robots.txt, rate-limits
/// requests, and only fetches detail pages for listings whose search-card relative-time label falls
/// within the requested age window. See OlxHtmlParser for the caveat on selector verification: this
/// sandboxed build environment cannot reach olx.pl at all (outbound web access is blocked by the
/// environment's network policy), so this class could only be exercised against synthetic fixture
/// HTML during development, never the live site. Run it locally before relying on it for a demo.
/// </summary>
public class OlxCrawler(HttpClient httpClient, IOptions<OlxCrawlerOptions> options, ILogger<OlxCrawler> logger) : IListingCrawler
{
    public async Task<CrawlResult> CrawlRecentListingsAsync(CrawlOptions crawlOptions, CancellationToken cancellationToken)
    {
        var config = options.Value;

        var robots = await FetchRobotsRulesAsync(cancellationToken);
        var searchPathOnly = config.SearchPath.Split('?')[0];
        if (!robots.IsAllowed(searchPathOnly))
        {
            throw new CrawlUnavailableException("robots.txt disallows crawling the configured search path.");
        }

        var requestDelay = robots.CrawlDelay ?? config.DefaultRequestDelay;

        var cards = await FetchRecentCardsAsync(config, requestDelay, crawlOptions, cancellationToken);
        if (cards.Count == 0)
        {
            throw new CrawlUnavailableException("No listing cards could be parsed from the OLX search results page; the site's markup may have changed.");
        }

        var listings = new List<Listing>();
        var attempted = 0;

        foreach (var card in cards.Take(config.MaxDetailFetches))
        {
            attempted++;
            await Task.Delay(requestDelay, cancellationToken);

            try
            {
                var listing = await FetchAndMapDetailAsync(card, config, cancellationToken);
                listings.Add(listing);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to parse OLX detail page {Url}; skipping this listing.", card.DetailUrl);
            }
        }

        var successRatio = attempted == 0 ? 0 : (double)listings.Count / attempted;
        if (successRatio < config.MinParseSuccessRatio)
        {
            throw new CrawlUnavailableException(
                $"Only {listings.Count}/{attempted} OLX detail pages parsed successfully ({successRatio:P0}); " +
                "treating the live crawl as unreliable (markup likely changed).");
        }

        return new CrawlResult(listings, Domain.Crawling.CrawlSourceUsed.Live,
            $"Fetched {listings.Count} listings from {attempted} OLX detail pages.");
    }

    private async Task<RobotsTxtRules> FetchRobotsRulesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var robotsTxt = await httpClient.GetStringAsync("/robots.txt", cancellationToken);
            return RobotsTxtParser.Parse(robotsTxt, options.Value.UserAgent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // If robots.txt itself can't be fetched, err on the side of not crawling.
            throw new CrawlUnavailableException("Could not fetch/parse OLX robots.txt.", ex);
        }
    }

    private async Task<List<OlxSearchResultCard>> FetchRecentCardsAsync(
        OlxCrawlerOptions config, TimeSpan requestDelay, CrawlOptions crawlOptions, CancellationToken cancellationToken)
    {
        var recentCards = new List<OlxSearchResultCard>();

        for (var page = 1; page <= config.MaxSearchPages; page++)
        {
            var separator = config.SearchPath.Contains('?') ? "&" : "?";
            var pageUrl = page == 1 ? config.SearchPath : $"{config.SearchPath}{separator}page={page}";

            string html;
            try
            {
                html = await httpClient.GetStringAsync(pageUrl, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new CrawlUnavailableException($"Failed to fetch OLX search results page {page}.", ex);
            }

            var cards = await OlxHtmlParser.ParseSearchResultsPageAsync(html, config.BaseUrl);
            var withinWindow = cards.Where(c => c.HoursAgo is { } h && TimeSpan.FromHours(h) <= crawlOptions.MaxAge).ToList();
            recentCards.AddRange(withinWindow);

            // Cards are sorted newest-first; once a page has none within the window, later pages won't either.
            if (withinWindow.Count == 0)
            {
                break;
            }

            if (page < config.MaxSearchPages)
            {
                await Task.Delay(requestDelay, cancellationToken);
            }
        }

        return recentCards;
    }

    private async Task<Listing> FetchAndMapDetailAsync(OlxSearchResultCard card, OlxCrawlerOptions config, CancellationToken cancellationToken)
    {
        var html = await httpClient.GetStringAsync(card.DetailUrl, cancellationToken);
        var (description, year, mileage, transmission, fuelType, bodyType, brand, model, imageUrls) =
            await OlxHtmlParser.ParseDetailPageAsync(html);

        var now = DateTimeOffset.UtcNow;

        return new Listing
        {
            Source = ListingSource.Olx,
            SourceListingId = card.SourceListingId,
            SourceUrl = card.DetailUrl,
            Title = card.Title,
            Description = description,
            Price = new Money(card.PriceAmount ?? 0, Currency.Pln),
            Year = year ?? now.Year,
            Mileage = mileage,
            Transmission = transmission,
            FuelType = fuelType,
            BodyType = bodyType,
            Brand = brand,
            Model = model,
            City = card.City ?? "Unknown",
            Region = null,
            PublishedAt = now - TimeSpan.FromHours(card.HoursAgo ?? 0),
            CrawledAt = now,
            ImageUrls = imageUrls.Count > 0 ? imageUrls : (card.ThumbnailUrl is not null ? [card.ThumbnailUrl] : [])
        };
    }
}
