namespace MojProjekt.Infrastructure.Crawling;

public enum CrawlerMode
{
    /// <summary>Try the live OLX crawler first, automatically fall back to sample data if it fails.</summary>
    Auto,

    /// <summary>Always use the live OLX crawler; surface failures instead of falling back.</summary>
    LiveOnly,

    /// <summary>Always use the bundled sample dataset. Useful for deterministic offline demos/tests.</summary>
    SampleOnly
}

public class CrawlerOptions
{
    public const string SectionName = "Crawler";

    public CrawlerMode Mode { get; set; } = CrawlerMode.Auto;
}
