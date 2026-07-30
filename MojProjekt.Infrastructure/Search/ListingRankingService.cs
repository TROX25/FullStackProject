using MojProjekt.Application.Common;
using MojProjekt.Application.Search;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Search;

/// <summary>
/// Deterministic scoring of listings against structured SearchCriteria. Every point awarded or
/// deducted has a corresponding human-readable reason, so results are always explainable and never
/// rely on a second AI call to describe "why this matched".
/// </summary>
public class ListingRankingService : IListingRankingService
{
    public IReadOnlyList<ScoredListing> Rank(IReadOnlyList<Listing> listings, SearchCriteria criteria, int maxResults)
    {
        var scored = new List<ScoredListing>();

        foreach (var listing in listings)
        {
            var reasons = new List<string>();
            var unmet = new List<string>();
            var score = 50;
            var excludedByHardRequirement = false;

            ScorePrice(listing, criteria, reasons, unmet, ref score);
            ScoreYear(listing, criteria, reasons, unmet, ref score);
            ScoreMileage(listing, criteria, reasons, unmet, ref score);
            ScoreEnumPreference(criteria.TransmissionPreference, listing.Transmission, "transmission",
                reasons, unmet, ref score, ref excludedByHardRequirement);
            ScoreEnumPreference(criteria.FuelTypePreference, listing.FuelType, "fuel type",
                reasons, unmet, ref score, ref excludedByHardRequirement);
            ScoreEnumPreference(criteria.BodyTypePreference, listing.BodyType, "body type",
                reasons, unmet, ref score, ref excludedByHardRequirement);
            ScoreBrandModel(listing, criteria, reasons, unmet, ref score);
            ScoreKeywords(listing, criteria, reasons, ref score);

            if (excludedByHardRequirement)
            {
                continue;
            }

            score = Math.Clamp(score, 0, 100);
            scored.Add(new ScoredListing(listing, score, reasons, unmet));
        }

        return scored
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Listing.PublishedAt)
            .Take(maxResults)
            .ToList();
    }

    private static void ScorePrice(Listing listing, SearchCriteria criteria, List<string> reasons, List<string> unmet, ref int score)
    {
        if (criteria.PriceMax is { } max)
        {
            if (listing.Price.Amount <= max)
            {
                score += 20;
                reasons.Add($"Within your budget of {max:N0} {listing.Price.Currency} (listed at {listing.Price.Amount:N0} {listing.Price.Currency}).");
            }
            else
            {
                var overBy = listing.Price.Amount - max;
                var overRatio = overBy / max;
                score -= (int)Math.Min(35, overRatio * 100);
                unmet.Add($"Over your budget of {max:N0} {listing.Price.Currency} by {overBy:N0} {listing.Price.Currency}.");
            }
        }

        if (criteria.PriceMin is { } min && listing.Price.Amount < min)
        {
            score -= 5;
            unmet.Add($"Priced below your stated minimum of {min:N0} {listing.Price.Currency}, which can indicate a different trim or condition.");
        }
    }

    private static void ScoreYear(Listing listing, SearchCriteria criteria, List<string> reasons, List<string> unmet, ref int score)
    {
        if (criteria.YearMin is { } yearMin)
        {
            if (listing.Year >= yearMin)
            {
                score += 15;
                reasons.Add($"{listing.Year} model meets your 'no older than {yearMin}' requirement.");
            }
            else
            {
                var yearsOff = yearMin - listing.Year;
                score -= Math.Min(25, yearsOff * 6);
                unmet.Add($"{listing.Year} model is {yearsOff} year(s) older than your '{yearMin} or newer' preference.");
            }
        }

        if (criteria.YearMax is { } yearMax && listing.Year > yearMax)
        {
            score -= 10;
            unmet.Add($"{listing.Year} model is newer than your stated maximum of {yearMax}.");
        }
    }

    private static void ScoreMileage(Listing listing, SearchCriteria criteria, List<string> reasons, List<string> unmet, ref int score)
    {
        if (criteria.MileageMax is not { } mileageMax || listing.Mileage is not { } mileage)
        {
            return;
        }

        if (mileage <= mileageMax)
        {
            score += 10;
            reasons.Add($"Mileage of {mileage:N0} km is within your {mileageMax:N0} km limit.");
        }
        else
        {
            score -= 10;
            unmet.Add($"Mileage of {mileage:N0} km exceeds your {mileageMax:N0} km limit.");
        }
    }

    private static void ScoreEnumPreference<T>(
        EnumPreference<T>? preference, T actualValue, string attributeName,
        List<string> reasons, List<string> unmet, ref int score, ref bool excludedByHardRequirement)
        where T : struct, Enum
    {
        if (preference is not { } pref)
        {
            return;
        }

        if (EqualityComparer<T>.Default.Equals(actualValue, pref.Value))
        {
            score += 10;
            reasons.Add($"{pref.Value} {attributeName} as requested.");
            return;
        }

        if (pref.IsRequired)
        {
            excludedByHardRequirement = true;
            return;
        }

        score -= 8;
        unmet.Add($"{actualValue} {attributeName} — you preferred {pref.Value}.");
    }

    private static void ScoreBrandModel(Listing listing, SearchCriteria criteria, List<string> reasons, List<string> unmet, ref int score)
    {
        if (!string.IsNullOrWhiteSpace(criteria.Brand))
        {
            if (string.Equals(listing.Brand, criteria.Brand, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
                reasons.Add($"Brand matches your request for {criteria.Brand}.");
            }
            else
            {
                score -= 15;
                unmet.Add($"{listing.Brand} does not match your requested brand ({criteria.Brand}).");
            }
        }

        if (!string.IsNullOrWhiteSpace(criteria.Model) &&
            listing.Model.Contains(criteria.Model, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
            reasons.Add($"Model matches your request for {criteria.Model}.");
        }
    }

    private static void ScoreKeywords(Listing listing, SearchCriteria criteria, List<string> reasons, ref int score)
    {
        if (criteria.Keywords.Count == 0)
        {
            return;
        }

        var haystack = $"{listing.Title} {listing.Description}".ToLowerInvariant();
        var matched = criteria.Keywords
            .Where(k => haystack.Contains(k, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count == 0)
        {
            return;
        }

        score += Math.Min(15, matched.Count * 3);
        reasons.Add($"Listing text mentions: {string.Join(", ", matched.Take(4))}.");
    }
}
