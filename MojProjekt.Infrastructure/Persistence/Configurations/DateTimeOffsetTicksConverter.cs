using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MojProjekt.Infrastructure.Persistence.Configurations;

/// <summary>
/// SQLite's EF Core provider can't translate ORDER BY/comparisons over DateTimeOffset columns. This
/// app only ever stores UTC instants, so round-tripping through UTC ticks (a plain INTEGER column)
/// keeps sorting/filtering in SQL without losing precision.
/// </summary>
public class DateTimeOffsetTicksConverter()
    : ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero))
{
    public static readonly DateTimeOffsetTicksConverter Instance = new();
}
