namespace MojProjekt.Application.Crawling.Contracts;

public sealed record CrawlRunDto(
    Guid Id,
    string Status,
    string SourceUsed,
    int ListingsFound,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
