using System.Text.Json;
using Microsoft.Extensions.Logging;
using MojProjekt.Application.Crawling;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Crawling.Fallback;

/// <summary>
/// Loads a bundled, realistic sample dataset instead of crawling OLX live. Used automatically by
/// CompositeListingCrawler when the live crawler is unavailable, and always used in SampleOnly mode
/// for a guaranteed deterministic demo.
/// </summary>
public class SampleDataCrawler(ILogger<SampleDataCrawler> logger) : IListingCrawler
{
    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "Crawling", "Fallback", "sample-listings.json");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<CrawlResult> CrawlRecentListingsAsync(CrawlOptions options, CancellationToken cancellationToken)
    {
        var entries = LoadFixtureEntries();
        var now = DateTimeOffset.UtcNow;

        var listings = entries
            .Where(e => TimeSpan.FromHours(e.HoursAgo) <= options.MaxAge)
            .Take(options.MaxListings)
            .Select(e => MapToListing(e, now))
            .ToList();

        logger.LogInformation("Sample fallback crawler produced {Count} listings.", listings.Count);

        return Task.FromResult(new CrawlResult(listings, Domain.Crawling.CrawlSourceUsed.Fallback,
            $"Loaded {listings.Count} listings from the bundled sample dataset."));
    }

    private static IReadOnlyList<SampleListingFixtureEntry> LoadFixtureEntries()
    {
        if (!File.Exists(FixturePath))
        {
            throw new CrawlUnavailableException($"Sample listing fixture not found at '{FixturePath}'.");
        }

        var json = File.ReadAllText(FixturePath);
        var entries = JsonSerializer.Deserialize<List<SampleListingFixtureEntry>>(json, JsonOptions);

        return entries ?? [];
    }

    private static Listing MapToListing(SampleListingFixtureEntry entry, DateTimeOffset now)
    {
        var publishedAt = now - TimeSpan.FromHours(entry.HoursAgo);

        return new Listing
        {
            Source = ListingSource.Sample,
            SourceListingId = entry.SourceListingId,
            SourceUrl = entry.SourceUrl,
            Title = entry.Title,
            Description = entry.Description,
            Price = new Money(entry.PriceAmount, Enum.Parse<Currency>(entry.Currency, ignoreCase: true)),
            Year = entry.Year,
            Mileage = entry.Mileage,
            Transmission = Enum.TryParse<Transmission>(entry.Transmission, ignoreCase: true, out var t) ? t : Transmission.Unknown,
            FuelType = Enum.TryParse<FuelType>(entry.FuelType, ignoreCase: true, out var f) ? f : FuelType.Unknown,
            BodyType = Enum.TryParse<BodyType>(entry.BodyType, ignoreCase: true, out var b) ? b : BodyType.Unknown,
            Brand = entry.Brand,
            Model = entry.Model,
            City = entry.City,
            Region = entry.Region,
            PublishedAt = publishedAt,
            CrawledAt = now,
            ImageUrls = entry.ImageUrls
        };
    }
}
