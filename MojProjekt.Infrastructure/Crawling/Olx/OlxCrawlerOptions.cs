namespace MojProjekt.Infrastructure.Crawling.Olx;

public class OlxCrawlerOptions
{
    public const string SectionName = "Olx";

    public string BaseUrl { get; set; } = "https://www.olx.pl";

    /// <summary>Passenger cars search, newest first.</summary>
    public string SearchPath { get; set; } = "/motoryzacja/samochody/?search%5Border%5D=created_at:desc";

    public string UserAgent { get; set; } = "MojProjektRecruitmentDemoBot/1.0 (+contact: recruitment-demo@example.com)";

    public int MaxSearchPages { get; set; } = 2;

    public int MaxDetailFetches { get; set; } = 30;

    public TimeSpan DefaultRequestDelay { get; set; } = TimeSpan.FromSeconds(2.5);

    /// <summary>Below this parse-success ratio for detail pages, treat the crawl as unreliable and fall back.</summary>
    public double MinParseSuccessRatio { get; set; } = 0.5;
}
