using MojProjekt.Application.Listings.Mapping;
using MojProjekt.Application.Search.Contracts;

namespace MojProjekt.Application.Search.Mapping;

public static class SearchMappingExtensions
{
    public static InterpretedCriteriaDto ToDto(this SearchCriteria criteria) => new(
        criteria.PriceMin,
        criteria.PriceMax,
        criteria.YearMin,
        criteria.YearMax,
        criteria.MileageMax,
        criteria.TransmissionPreference?.Value.ToString(),
        criteria.TransmissionPreference?.IsRequired ?? false,
        criteria.FuelTypePreference?.Value.ToString(),
        criteria.FuelTypePreference?.IsRequired ?? false,
        criteria.BodyTypePreference?.Value.ToString(),
        criteria.BodyTypePreference?.IsRequired ?? false,
        criteria.Brand,
        criteria.Model,
        criteria.Keywords);

    public static SearchResultItemDto ToDto(this ScoredListing scored) => new(
        scored.Listing.ToSummaryDto(),
        scored.Score,
        scored.MatchReasons,
        scored.UnmetPreferences);
}
