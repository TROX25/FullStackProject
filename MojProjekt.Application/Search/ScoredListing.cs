using MojProjekt.Domain.Listings;

namespace MojProjekt.Application.Search;

public sealed record ScoredListing(
    Listing Listing,
    int Score,
    IReadOnlyList<string> MatchReasons,
    IReadOnlyList<string> UnmetPreferences);
