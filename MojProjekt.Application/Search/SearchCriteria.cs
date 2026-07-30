using MojProjekt.Application.Common;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Application.Search;

/// <summary>
/// Structured, explainable representation of a natural-language search query, extracted by an
/// INaturalLanguageQueryInterpreter. Every field is optional because a user's query may only
/// specify some of them.
/// </summary>
public sealed record SearchCriteria(
    decimal? PriceMin,
    decimal? PriceMax,
    int? YearMin,
    int? YearMax,
    int? MileageMax,
    EnumPreference<Transmission>? TransmissionPreference,
    EnumPreference<FuelType>? FuelTypePreference,
    EnumPreference<BodyType>? BodyTypePreference,
    string? Brand,
    string? Model,
    IReadOnlyList<string> Keywords)
{
    public static SearchCriteria Empty { get; } = new(
        null, null, null, null, null, null, null, null, null, null, []);
}
