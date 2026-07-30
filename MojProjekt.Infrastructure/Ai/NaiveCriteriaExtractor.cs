using System.Text.RegularExpressions;
using MojProjekt.Application.Common;
using MojProjekt.Application.Search;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Ai;

/// <summary>
/// Regex/keyword-based fallback used when the AI query interpreter is unavailable (no API key
/// configured, network error, malformed AI response). Deliberately simple: it only needs to keep
/// search "working, just less smart" rather than fully replicate AI-quality understanding.
/// </summary>
public static class NaiveCriteriaExtractor
{
    private static readonly Regex PriceMaxRegex = new(
        @"(?:under|below|max(?:imum)?|up to|do|ponizej|poniżej)\s*(?:pln|zl|zł)?\s*([\d\s,.]{2,10})\s*(?:pln|zl|zł)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YearMinRegex = new(
        @"(?:no older than|not older than|since|after|od|nie starsz\w*\s*niż)\s*(\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BareYearRegex = new(@"\b(19[8-9]\d|20[0-4]\d)\b", RegexOptions.Compiled);

    public static SearchCriteria Extract(string query)
    {
        var lower = query.ToLowerInvariant();

        decimal? priceMax = null;
        var priceMatch = PriceMaxRegex.Match(lower);
        if (priceMatch.Success)
        {
            var digits = priceMatch.Groups[1].Value.Replace(" ", "").Replace(",", "").Replace(".", "");
            if (decimal.TryParse(digits, out var parsedPrice))
            {
                priceMax = parsedPrice;
            }
        }

        int? yearMin = null;
        var yearMatch = YearMinRegex.Match(lower);
        if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var parsedYear))
        {
            yearMin = parsedYear;
        }
        else
        {
            var bareYear = BareYearRegex.Match(lower);
            if (bareYear.Success && int.TryParse(bareYear.Value, out var parsedBareYear))
            {
                yearMin = parsedBareYear;
            }
        }

        var transmission = ExtractEnumPreference(lower,
            required: ["must be automatic", "musi byc automatyczna", "wymagana automatyczna"],
            preferred: ["automatic", "automat", "automatyczna"],
            value: Transmission.Automatic)
            ?? ExtractEnumPreference(lower,
                required: ["must be manual", "musi byc manualna"],
                preferred: ["manual", "manualna"],
                value: Transmission.Manual);

        var fuelType = ExtractEnumPreference(lower, [], ["diesel"], FuelType.Diesel)
            ?? ExtractEnumPreference(lower, [], ["petrol", "benzyna", "gasoline"], FuelType.Petrol)
            ?? ExtractEnumPreference(lower, [], ["hybrid", "hybryda"], FuelType.Hybrid)
            ?? ExtractEnumPreference(lower, [], ["electric", "elektryczny", "ev"], FuelType.Electric)
            ?? ExtractEnumPreference(lower, [], ["lpg", "gaz"], FuelType.Lpg);

        var bodyType = ExtractEnumPreference(lower, [], ["estate", "kombi", "wagon"], BodyType.Estate)
            ?? ExtractEnumPreference(lower, [], ["suv"], BodyType.Suv)
            ?? ExtractEnumPreference(lower, [], ["hatchback"], BodyType.Hatchback)
            ?? ExtractEnumPreference(lower, [], ["sedan"], BodyType.Sedan)
            ?? ExtractEnumPreference(lower, [], ["coupe", "kupe"], BodyType.Coupe)
            ?? ExtractEnumPreference(lower, [], ["van", "minivan"], BodyType.Van)
            ?? ExtractEnumPreference(lower, [], ["pickup"], BodyType.Pickup)
            ?? ExtractEnumPreference(lower, [], ["convertible", "kabriolet"], BodyType.Convertible);

        var keywords = query
            .Split([' ', ',', '.', '"'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        return new SearchCriteria(
            PriceMin: null,
            PriceMax: priceMax,
            YearMin: yearMin,
            YearMax: null,
            MileageMax: null,
            TransmissionPreference: transmission,
            FuelTypePreference: fuelType,
            BodyTypePreference: bodyType,
            Brand: null,
            Model: null,
            Keywords: keywords);
    }

    private static EnumPreference<T>? ExtractEnumPreference<T>(
        string lowerQuery, string[] required, string[] preferred, T value) where T : struct, Enum
    {
        if (required.Any(lowerQuery.Contains))
        {
            return new EnumPreference<T>(value, IsRequired: true);
        }

        if (preferred.Any(lowerQuery.Contains))
        {
            var isRequired = lowerQuery.Contains("must") || lowerQuery.Contains("musi") || lowerQuery.Contains("wymagan");
            return new EnumPreference<T>(value, isRequired);
        }

        return null;
    }
}
