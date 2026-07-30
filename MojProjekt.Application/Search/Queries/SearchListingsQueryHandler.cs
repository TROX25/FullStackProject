using MediatR;
using MojProjekt.Application.Listings;
using MojProjekt.Application.Search.Contracts;
using MojProjekt.Application.Search.Mapping;

namespace MojProjekt.Application.Search.Queries;

public sealed class SearchListingsQueryHandler(
    INaturalLanguageQueryInterpreter interpreter,
    IListingRepository listingRepository,
    IListingRankingService rankingService)
    : IRequestHandler<SearchListingsQuery, SearchResponseDto>
{
    public async Task<SearchResponseDto> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
    {
        var interpretation = await interpreter.InterpretAsync(request.Query, cancellationToken);

        var listings = await listingRepository.GetAllAsync(cancellationToken);

        var ranked = rankingService.Rank(listings, interpretation.Criteria, request.MaxResults);

        var warnings = interpretation.UsedFallbackExtraction
            ? [.. interpretation.Warnings, "AI query interpretation was unavailable; used a simplified keyword-based fallback instead."]
            : interpretation.Warnings;

        return new SearchResponseDto(
            interpretation.IntentSummary,
            interpretation.Criteria.ToDto(),
            warnings,
            ranked.Select(r => r.ToDto()).ToList());
    }
}
