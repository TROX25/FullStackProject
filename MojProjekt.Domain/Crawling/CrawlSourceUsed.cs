namespace MojProjekt.Domain.Crawling;

public enum CrawlSourceUsed
{
    /// <summary>No source recorded yet (run still pending/in progress).</summary>
    None,
    Live,
    Fallback
}
