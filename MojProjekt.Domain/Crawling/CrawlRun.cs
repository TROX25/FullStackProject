namespace MojProjekt.Domain.Crawling;

public class CrawlRun
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public CrawlStatus Status { get; set; } = CrawlStatus.Pending;

    public CrawlSourceUsed SourceUsed { get; set; } = CrawlSourceUsed.None;

    public int ListingsFound { get; set; }

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
}
