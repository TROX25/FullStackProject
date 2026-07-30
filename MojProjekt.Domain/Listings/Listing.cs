namespace MojProjekt.Domain.Listings;

public class Listing
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required ListingSource Source { get; init; }

    /// <summary>The source portal's own id for the listing, used for dedup on re-crawl.</summary>
    public required string SourceListingId { get; init; }

    public required string SourceUrl { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required Money Price { get; init; }

    public required int Year { get; init; }

    public int? Mileage { get; init; }

    public Transmission Transmission { get; init; } = Transmission.Unknown;

    public FuelType FuelType { get; init; } = FuelType.Unknown;

    public BodyType BodyType { get; init; } = BodyType.Unknown;

    public required string Brand { get; init; }

    public required string Model { get; init; }

    public required string City { get; init; }

    public string? Region { get; init; }

    public required DateTimeOffset PublishedAt { get; init; }

    public required DateTimeOffset CrawledAt { get; init; }

    public IReadOnlyList<string> ImageUrls { get; init; } = [];

    /// <summary>
    /// Returns a copy of this listing with a different Id, keeping every other field. Used when
    /// upserting a freshly-crawled listing (which always gets a new random Id at construction time)
    /// onto an already-stored row matched by (Source, SourceListingId) — EF Core's
    /// EntityEntry.CurrentValues.SetValues throws if the source object's key doesn't match the
    /// tracked entity's key, so the incoming listing must be re-keyed before being used to update it.
    /// </summary>
    public Listing WithId(Guid id) => new()
    {
        Id = id,
        Source = Source,
        SourceListingId = SourceListingId,
        SourceUrl = SourceUrl,
        Title = Title,
        Description = Description,
        Price = Price,
        Year = Year,
        Mileage = Mileage,
        Transmission = Transmission,
        FuelType = FuelType,
        BodyType = BodyType,
        Brand = Brand,
        Model = Model,
        City = City,
        Region = Region,
        PublishedAt = PublishedAt,
        CrawledAt = CrawledAt,
        ImageUrls = ImageUrls
    };
}
