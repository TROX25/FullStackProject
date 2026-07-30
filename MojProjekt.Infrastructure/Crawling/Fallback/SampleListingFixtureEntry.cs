namespace MojProjekt.Infrastructure.Crawling.Fallback;

/// <summary>
/// Shape of an entry in sample-listings.json. HoursAgo (rather than an absolute timestamp) is
/// converted to PublishedAt relative to "now" at load time, so the bundled fixture always looks
/// freshly crawled regardless of when the demo is actually run.
/// </summary>
public sealed class SampleListingFixtureEntry
{
    public required string SourceListingId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required decimal PriceAmount { get; init; }
    public required string Currency { get; init; }
    public required int Year { get; init; }
    public int? Mileage { get; init; }
    public required string Transmission { get; init; }
    public required string FuelType { get; init; }
    public required string BodyType { get; init; }
    public required string Brand { get; init; }
    public required string Model { get; init; }
    public required string City { get; init; }
    public string? Region { get; init; }
    public required double HoursAgo { get; init; }
    public required string SourceUrl { get; init; }
    public IReadOnlyList<string> ImageUrls { get; init; } = [];
}
