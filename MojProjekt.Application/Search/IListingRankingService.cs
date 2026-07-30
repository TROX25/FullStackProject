using MojProjekt.Domain.Listings;

namespace MojProjekt.Application.Search;

/// <summary>
/// Deterministic, non-AI scoring of listings against structured search criteria. Kept separate from
/// the AI query interpreter so that "why this matched" explanations are always traceable to code,
/// never hallucinated text from a model.
/// </summary>
public interface IListingRankingService
{
    IReadOnlyList<ScoredListing> Rank(IReadOnlyList<Listing> listings, SearchCriteria criteria, int maxResults);
}
