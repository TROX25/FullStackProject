using Microsoft.EntityFrameworkCore;
using MojProjekt.Domain.Crawling;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<CrawlRun> CrawlRuns => Set<CrawlRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
