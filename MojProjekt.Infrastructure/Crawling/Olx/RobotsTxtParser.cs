using System.Globalization;

namespace MojProjekt.Infrastructure.Crawling.Olx;

/// <summary>
/// Minimal robots.txt parser: extracts the Disallow/Allow/Crawl-delay directives that apply to a
/// specific user-agent (falling back to the "*" group when no group matches by name). Deliberately
/// small — this app only needs to answer "is this one path allowed" and "how fast may we crawl",
/// not implement the full robots.txt spec.
/// </summary>
public static class RobotsTxtParser
{
    public static RobotsTxtRules Parse(string robotsTxtContent, string userAgent)
    {
        var lines = robotsTxtContent
            .Split('\n')
            .Select(l => l.Split('#')[0].Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var groups = new List<(List<string> Agents, List<(string Directive, string Value)> Directives)>();
        List<string>? currentAgents = null;
        List<(string, string)>? currentDirectives = null;

        foreach (var line in lines)
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0)
            {
                continue;
            }

            var field = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (field.Equals("User-agent", StringComparison.OrdinalIgnoreCase))
            {
                if (currentAgents is null || currentDirectives!.Count > 0)
                {
                    currentAgents = [];
                    currentDirectives = [];
                    groups.Add((currentAgents, currentDirectives));
                }

                currentAgents.Add(value);
            }
            else if (currentDirectives is not null)
            {
                currentDirectives.Add((field, value));
            }
        }

        var matchingGroup = groups.FirstOrDefault(g => g.Agents.Any(a =>
            a.Equals(userAgent, StringComparison.OrdinalIgnoreCase)));

        if (matchingGroup.Directives is null)
        {
            matchingGroup = groups.FirstOrDefault(g => g.Agents.Contains("*"));
        }

        var directives = matchingGroup.Directives ?? [];

        var disallow = directives.Where(d => d.Directive.Equals("Disallow", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.Value).Where(v => v.Length > 0).ToList();
        var allow = directives.Where(d => d.Directive.Equals("Allow", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.Value).ToList();
        var crawlDelayValue = directives.FirstOrDefault(d => d.Directive.Equals("Crawl-delay", StringComparison.OrdinalIgnoreCase)).Value;

        TimeSpan? crawlDelay = double.TryParse(crawlDelayValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;

        return new RobotsTxtRules(disallow, allow, crawlDelay);
    }
}
