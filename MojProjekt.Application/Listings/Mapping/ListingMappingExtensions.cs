using MojProjekt.Application.Listings.Contracts;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Application.Listings.Mapping;

public static class ListingMappingExtensions
{
    public static ListingSummaryDto ToSummaryDto(this Listing listing) => new(
        listing.Id,
        listing.Title,
        listing.Price.Amount,
        listing.Price.Currency.ToString(),
        listing.Year,
        listing.Mileage,
        listing.Transmission.ToString(),
        listing.FuelType.ToString(),
        listing.BodyType.ToString(),
        listing.Brand,
        listing.Model,
        listing.City,
        listing.PublishedAt,
        listing.ImageUrls.Count > 0 ? listing.ImageUrls[0] : null,
        listing.SourceUrl);

    public static ListingDetailDto ToDetailDto(this Listing listing) => new(
        listing.Id,
        listing.Title,
        listing.Description,
        listing.Price.Amount,
        listing.Price.Currency.ToString(),
        listing.Year,
        listing.Mileage,
        listing.Transmission.ToString(),
        listing.FuelType.ToString(),
        listing.BodyType.ToString(),
        listing.Brand,
        listing.Model,
        listing.City,
        listing.Region,
        listing.PublishedAt,
        listing.ImageUrls,
        listing.SourceUrl);
}
