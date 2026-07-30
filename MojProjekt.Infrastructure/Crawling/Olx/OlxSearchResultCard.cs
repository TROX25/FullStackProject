namespace MojProjekt.Infrastructure.Crawling.Olx;

public sealed record OlxSearchResultCard(
    string SourceListingId,
    string Title,
    decimal? PriceAmount,
    string? City,
    double? HoursAgo,
    string DetailUrl,
    string? ThumbnailUrl);
