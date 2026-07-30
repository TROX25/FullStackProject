namespace MojProjekt.Application.Listings.Contracts;

public sealed record ListingSummaryDto(
    Guid Id,
    string Title,
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
    DateTimeOffset PublishedAt,
    string? ThumbnailUrl,
    string SourceUrl);
