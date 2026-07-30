using MojProjekt.Infrastructure.Crawling.Olx;

namespace MojProjekt.Infrastructure.Tests.Crawling.Olx;

public class RobotsTxtParserTests
{
    private const string SampleRobotsTxt = """
        User-agent: BadBot
        Disallow: /

        User-agent: *
        Disallow: /admin/
        Disallow: /motoryzacja/samochody/prywatne/
        Allow: /motoryzacja/samochody/
        Crawl-delay: 3
        """;

    [Fact]
    public void Parse_UnknownUserAgent_FallsBackToWildcardGroup()
    {
        var rules = RobotsTxtParser.Parse(SampleRobotsTxt, "MojProjektRecruitmentDemoBot/1.0");

        Assert.True(rules.IsAllowed("/motoryzacja/samochody/"));
        Assert.False(rules.IsAllowed("/admin/"));
    }

    [Fact]
    public void Parse_MoreSpecificAllowOverridesShorterDisallow()
    {
        var rules = RobotsTxtParser.Parse(SampleRobotsTxt, "MojProjektRecruitmentDemoBot/1.0");

        // No rule at all targets this exact path, so it should be allowed by default.
        Assert.True(rules.IsAllowed("/motoryzacja/samochody/osobowe/"));
    }

    [Fact]
    public void Parse_LongerDisallowWinsOverShorterAllow()
    {
        var rules = RobotsTxtParser.Parse(SampleRobotsTxt, "MojProjektRecruitmentDemoBot/1.0");

        Assert.False(rules.IsAllowed("/motoryzacja/samochody/prywatne/"));
    }

    [Fact]
    public void Parse_CrawlDelay_IsExtracted()
    {
        var rules = RobotsTxtParser.Parse(SampleRobotsTxt, "MojProjektRecruitmentDemoBot/1.0");

        Assert.Equal(TimeSpan.FromSeconds(3), rules.CrawlDelay);
    }

    [Fact]
    public void Parse_PathWithNoMatchingRuleAtAll_IsAllowedByDefault()
    {
        var rules = RobotsTxtParser.Parse(SampleRobotsTxt, "MojProjektRecruitmentDemoBot/1.0");

        Assert.True(rules.IsAllowed("/nieruchomosci/"));
    }

    [Fact]
    public void Parse_EmptyRobotsTxt_AllowsEverything()
    {
        var rules = RobotsTxtParser.Parse(string.Empty, "MojProjektRecruitmentDemoBot/1.0");

        Assert.True(rules.IsAllowed("/anything/"));
        Assert.Null(rules.CrawlDelay);
    }
}
