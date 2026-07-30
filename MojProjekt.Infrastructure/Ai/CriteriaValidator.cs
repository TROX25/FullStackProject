using MojProjekt.Application.Common;
using MojProjekt.Application.Search;
using MojProjekt.Domain.Listings;
using MojProjekt.Infrastructure.Ai.Anthropic;

namespace MojProjekt.Infrastructure.Ai;

/// <summary>
/// Validates and clamps the raw payload Claude returns from the extract_search_criteria tool before
/// it's trusted anywhere downstream. Never throws — always returns best-effort criteria plus a list
/// of warnings describing anything it had to reject or adjust, so a malformed/out-of-range AI
/// response degrades gracefully instead of corrupting search results.
/// </summary>
public static class CriteriaValidator
{
    private const int MinYear = 1970;

    public static (SearchCriteria Criteria, IReadOnlyList<string> Warnings) Validate(ExtractedCriteriaPayload payload)
    {
        var warnings = new List<string>();
        var maxYear = DateTime.UtcNow.Year + 1;

        var priceMin = ClampNonNegative(payload.PriceMin, "priceMin", warnings);
        var priceMax = ClampNonNegative(payload.PriceMax, "priceMax", warnings);
        if (priceMin.HasValue && priceMax.HasValue && priceMin > priceMax)
        {
            warnings.Add("priceMin was greater than priceMax; ignoring priceMin.");
            priceMin = null;
        }

        var yearMin = ClampYear(payload.YearMin, "yearMin", maxYear, warnings);
        var yearMax = ClampYear(payload.YearMax, "yearMax", maxYear, warnings);

        var mileageMax = ClampNonNegative(payload.MileageMax.HasValue ? (decimal)payload.MileageMax.Value : null, "mileageMax", warnings) is { } m
            ? (int)m
            : (int?)null;

        var transmission = ParseEnumPreference<Transmission>(payload.Transmission, payload.TransmissionRequired, "transmission", warnings);
        var fuelType = ParseEnumPreference<FuelType>(payload.FuelType, payload.FuelTypeRequired, "fuelType", warnings);
        var bodyType = ParseEnumPreference<BodyType>(payload.BodyType, payload.BodyTypeRequired, "bodyType", warnings);

        var keywords = (payload.Keywords ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Take(15)
            .ToList();

        var criteria = new SearchCriteria(
            priceMin, priceMax, yearMin, yearMax, mileageMax,
            transmission, fuelType, bodyType,
            string.IsNullOrWhiteSpace(payload.Brand) ? null : payload.Brand.Trim(),
            string.IsNullOrWhiteSpace(payload.Model) ? null : payload.Model.Trim(),
            keywords);

        return (criteria, warnings);
    }

    private static decimal? ClampNonNegative(decimal? value, string fieldName, List<string> warnings)
    {
        if (value is not { } v)
        {
            return null;
        }

        if (v < 0)
        {
            warnings.Add($"{fieldName} was negative ({v}); ignoring it.");
            return null;
        }

        return v;
    }

    private static int? ClampYear(int? value, string fieldName, int maxYear, List<string> warnings)
    {
        if (value is not { } v)
        {
            return null;
        }

        if (v < MinYear || v > maxYear)
        {
            warnings.Add($"{fieldName} ({v}) was outside the plausible range {MinYear}-{maxYear}; ignoring it.");
            return null;
        }

        return v;
    }

    private static EnumPreference<T>? ParseEnumPreference<T>(string? rawValue, bool isRequired, string fieldName, List<string> warnings)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (Enum.TryParse<T>(rawValue, ignoreCase: true, out var parsed) && parsed.ToString() != "Unknown")
        {
            return new EnumPreference<T>(parsed, isRequired);
        }

        warnings.Add($"{fieldName} value '{rawValue}' from the AI response was not recognized; ignoring it.");
        return null;
    }
}
