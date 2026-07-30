using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MojProjekt.Domain.Crawling;

namespace MojProjekt.Infrastructure.Persistence.Configurations;

public class CrawlRunConfiguration : IEntityTypeConfiguration<CrawlRun>
{
    public void Configure(EntityTypeBuilder<CrawlRun> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>();
        builder.Property(r => r.SourceUsed).HasConversion<string>();
        builder.Property(r => r.ErrorMessage).HasMaxLength(2000);

        builder.Property(r => r.StartedAt).HasConversion(DateTimeOffsetTicksConverter.Instance);
        builder.Property(r => r.CompletedAt).HasConversion(
            v => v.HasValue ? v.Value.UtcTicks : (long?)null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

        builder.HasIndex(r => r.StartedAt);
    }
}
