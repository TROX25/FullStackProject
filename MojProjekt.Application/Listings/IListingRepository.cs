using MojProjekt.Domain.Listings;

namespace MojProjekt.Application.Listings;

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Loads the full local dataset for in-memory search/ranking. Only safe at demo-scale datasets.</summary>
    Task<IReadOnlyList<Listing>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Inserts new listings and updates existing ones matched by (Source, SourceListingId).</summary>
    Task UpsertRangeAsync(IReadOnlyList<Listing> listings, CancellationToken cancellationToken);
}
