using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Persistence.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.SourceListingId).IsRequired().HasMaxLength(200);
        builder.Property(l => l.SourceUrl).IsRequired().HasMaxLength(1000);
        builder.Property(l => l.Title).IsRequired().HasMaxLength(300);
        builder.Property(l => l.Brand).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Model).IsRequired().HasMaxLength(100);
        builder.Property(l => l.City).IsRequired().HasMaxLength(150);

        builder.Property(l => l.Source).HasConversion<string>();
        builder.Property(l => l.Transmission).HasConversion<string>();
        builder.Property(l => l.FuelType).HasConversion<string>();
        builder.Property(l => l.BodyType).HasConversion<string>();

        // SQLite can't translate ORDER BY/comparisons over DateTimeOffset directly; store as UTC ticks
        // (always UTC in this app) so sorting/filtering happens in SQL instead of failing at query time.
        builder.Property(l => l.PublishedAt).HasConversion(DateTimeOffsetTicksConverter.Instance);
        builder.Property(l => l.CrawledAt).HasConversion(DateTimeOffsetTicksConverter.Instance);

        builder.ComplexProperty(l => l.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("PriceAmount").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("PriceCurrency").HasConversion<string>();
        });

        var imageUrlsComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        builder.Property(l => l.ImageUrls)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>())
            .Metadata.SetValueComparer(imageUrlsComparer);

        builder.HasIndex(l => new { l.Source, l.SourceListingId }).IsUnique();
        builder.HasIndex(l => l.PublishedAt);
    }
}
