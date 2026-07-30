using Microsoft.EntityFrameworkCore;
using MojProjekt.Application.Listings;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Persistence;

public class ListingRepository(AppDbContext dbContext) : IListingRepository
{
    public Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Listings.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Listings.OrderByDescending(l => l.PublishedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Listing>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Listings.AsNoTracking().ToListAsync(cancellationToken);

    public async Task UpsertRangeAsync(IReadOnlyList<Listing> listings, CancellationToken cancellationToken)
    {
        foreach (var listing in listings)
        {
            var existing = await dbContext.Listings.FirstOrDefaultAsync(
                l => l.Source == listing.Source && l.SourceListingId == listing.SourceListingId,
                cancellationToken);

            if (existing is null)
            {
                dbContext.Listings.Add(listing);
            }
            else
            {
                // CurrentValues.SetValues throws if the source object's key differs from the tracked
                // entity's key, and a freshly-crawled `listing` always has its own new Id — re-key it
                // to match `existing` before copying values over.
                dbContext.Entry(existing).CurrentValues.SetValues(listing.WithId(existing.Id));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
