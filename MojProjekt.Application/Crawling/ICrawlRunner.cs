namespace MojProjekt.Application.Crawling;

/// <summary>
/// Executes a previously-created CrawlRun (crawl → map → upsert → mark Completed/Failed) outside the
/// request/response cycle of the endpoint that triggered it. Implemented in Infrastructure using
/// IServiceScopeFactory so the background work gets its own scoped dependencies (DbContext, etc.).
/// </summary>
public interface ICrawlRunner
{
    void RunInBackground(Guid crawlRunId);
}
