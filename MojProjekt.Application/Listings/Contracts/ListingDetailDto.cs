namespace MojProjekt.Application.Listings.Contracts;

public sealed record ListingDetailDto(
    Guid Id,
    string Title,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int Year,
    int? Mileage,
    string Transmission,
    string FuelType,
    string BodyType,
    string Brand,
    string Model,
    string City,
    string? Region,
    DateTimeOffset PublishedAt,
    IReadOnlyList<string> ImageUrls,
    string SourceUrl);
