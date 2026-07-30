using MojProjekt.Application.Listings.Contracts;

namespace MojProjekt.Application.Search.Contracts;

public sealed record InterpretedCriteriaDto(
    decimal? PriceMin,
    decimal? PriceMax,
    int? YearMin,
    int? YearMax,
    int? MileageMax,
    string? Transmission,
    bool TransmissionRequired,
    string? FuelType,
    bool FuelTypeRequired,
    string? BodyType,
    bool BodyTypeRequired,
    string? Brand,
    string? Model,
    IReadOnlyList<string> Keywords);

public sealed record SearchResultItemDto(
    ListingSummaryDto Listing,
    int Score,
    IReadOnlyList<string> MatchReasons,
    IReadOnlyList<string> UnmetPreferences);

public sealed record SearchResponseDto(
    string IntentSummary,
    InterpretedCriteriaDto InterpretedCriteria,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SearchResultItemDto> Results);
