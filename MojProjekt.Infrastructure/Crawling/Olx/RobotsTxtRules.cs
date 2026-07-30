namespace MojProjekt.Infrastructure.Crawling.Olx;

public sealed record RobotsTxtRules(IReadOnlyList<string> DisallowPaths, IReadOnlyList<string> AllowPaths, TimeSpan? CrawlDelay)
{
    /// <summary>
    /// Standard robots.txt precedence: the longest matching Allow/Disallow rule wins; no matching
    /// rule means allowed by default.
    /// </summary>
    public bool IsAllowed(string path)
    {
        var bestDisallowLength = DisallowPaths.Where(path.StartsWith).Select(p => p.Length).DefaultIfEmpty(-1).Max();
        var bestAllowLength = AllowPaths.Where(path.StartsWith).Select(p => p.Length).DefaultIfEmpty(-1).Max();

        return bestDisallowLength <= bestAllowLength;
    }
}
