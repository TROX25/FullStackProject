using Microsoft.EntityFrameworkCore;
using MojProjekt.Domain.Listings;
using MojProjekt.Infrastructure.Persistence;

namespace MojProjekt.Infrastructure.Tests.Persistence;

/// <summary>
/// Regression coverage for a real bug found during manual verification: re-crawling already-stored
/// listings threw because EntityEntry.CurrentValues.SetValues rejects a source object whose key
/// (Listing.Id) differs from the tracked entity's key — every freshly-crawled Listing gets its own
/// new Id, so upserting by (Source, SourceListingId) always hit this. Fixed via Listing.WithId(); see
/// ListingRepository.UpsertRangeAsync and AI-WORKFLOW.md for the story of how this was caught.
/// </summary>
public class ListingRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"repo-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _dbContext;
    private readonly ListingRepository _sut;

    public ListingRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.Migrate();
        _sut = new ListingRepository(_dbContext);
    }

    private static Listing MakeListing(string sourceListingId, decimal price) => new()
    {
        Source = ListingSource.Sample,
        SourceListingId = sourceListingId,
        SourceUrl = "https://example.test/listing",
        Title = "Skoda Octavia",
        Price = new Money(price, Currency.Pln),
        Year = 2020,
        Brand = "Skoda",
        Model = "Octavia",
        City = "Warszawa",
        PublishedAt = DateTimeOffset.UtcNow,
        CrawledAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task UpsertRangeAsync_SameSourceListingIdTwice_UpdatesInPlaceWithoutThrowing()
    {
        await _sut.UpsertRangeAsync([MakeListing("dup-1", price: 40_000)], CancellationToken.None);

        var exception = await Record.ExceptionAsync(() =>
            _sut.UpsertRangeAsync([MakeListing("dup-1", price: 42_000)], CancellationToken.None));

        Assert.Null(exception);

        var (items, totalCount) = await _sut.GetPagedAsync(1, 10, CancellationToken.None);
        Assert.Equal(1, totalCount);
        Assert.Equal(42_000, items.Single().Price.Amount);
    }

    [Fact]
    public async Task UpsertRangeAsync_NewSourceListingId_InsertsNewRow()
    {
        await _sut.UpsertRangeAsync([MakeListing("a", 10_000)], CancellationToken.None);
        await _sut.UpsertRangeAsync([MakeListing("b", 20_000)], CancellationToken.None);

        var (_, totalCount) = await _sut.GetPagedAsync(1, 10, CancellationToken.None);
        Assert.Equal(2, totalCount);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
