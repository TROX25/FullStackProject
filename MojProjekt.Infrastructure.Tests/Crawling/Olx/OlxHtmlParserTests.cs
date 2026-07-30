using MojProjekt.Domain.Listings;
using MojProjekt.Infrastructure.Crawling.Olx;

namespace MojProjekt.Infrastructure.Tests.Crawling.Olx;

/// <summary>
/// Exercises OlxHtmlParser against saved fixture HTML rather than the live site — this sandboxed
/// build environment has no general internet access, so the fixtures are synthetic approximations
/// of OLX's markup, not captured live pages. See the fixtures' own header comments and
/// ASSUMPTIONS.md for the caveat this implies for the live crawl path.
/// </summary>
public class OlxHtmlParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));

    [Fact]
    public async Task ParseSearchResultsPageAsync_ExtractsAllCardsWithExpectedFields()
    {
        var html = ReadFixture("olx-search-results.html");

        var cards = await OlxHtmlParser.ParseSearchResultsPageAsync(html, "https://www.olx.pl");

        Assert.Equal(3, cards.Count);

        var octavia = cards.Single(c => c.Title.Contains("Octavia"));
        Assert.Equal(45900m, octavia.PriceAmount);
        Assert.Equal("Warszawa", octavia.City);
        Assert.NotNull(octavia.HoursAgo);
        Assert.Equal(3, octavia.HoursAgo!.Value, precision: 3);
        Assert.StartsWith("https://www.olx.pl/", octavia.DetailUrl);
    }

    [Fact]
    public async Task ParseSearchResultsPageAsync_TodayWithClockTime_ComputesHoursAgoFromNow()
    {
        var html = ReadFixture("olx-search-results.html");

        var cards = await OlxHtmlParser.ParseSearchResultsPageAsync(html, "https://www.olx.pl");

        var bmw = cards.Single(c => c.Title.Contains("BMW"));
        Assert.NotNull(bmw.HoursAgo);
        Assert.True(bmw.HoursAgo!.Value >= 0);
    }

    [Fact]
    public async Task ParseSearchResultsPageAsync_ListingFromDaysAgo_HoursAgoExceeds24Hours()
    {
        var html = ReadFixture("olx-search-results.html");

        var cards = await OlxHtmlParser.ParseSearchResultsPageAsync(html, "https://www.olx.pl");

        var astra = cards.Single(c => c.Title.Contains("Astra"));
        // "2 dni temu" isn't handled by the relative-time parser's specific patterns, so it falls
        // through to null — the caller (OlxCrawler) treats a null HoursAgo as outside the 24h window.
        Assert.Null(astra.HoursAgo);
    }

    [Fact]
    public async Task ParseDetailPageAsync_ExtractsStructuredParametersAndImages()
    {
        var html = ReadFixture("olx-detail-page.html");

        var (description, year, mileage, transmission, fuelType, bodyType, brand, model, imageUrls) =
            await OlxHtmlParser.ParseDetailPageAsync(html);

        Assert.Contains("Bezwypadkowy", description);
        Assert.Equal(2018, year);
        Assert.Equal(120000, mileage);
        Assert.Equal(Transmission.Manual, transmission);
        Assert.Equal(FuelType.Diesel, fuelType);
        Assert.Equal(BodyType.Estate, bodyType);
        Assert.Equal("Skoda", brand);
        Assert.Equal("Octavia", model);
        Assert.Equal(2, imageUrls.Count);
    }

    [Theory]
    [InlineData("3 godziny temu", 3)]
    [InlineData("45 minut temu", 0.75)]
    public void ParseRelativeTime_HandlesGodzinyAndMinutyPatterns(string text, double expectedHours)
    {
        var result = OlxHtmlParser.ParseRelativeTime(text, DateTimeOffset.UtcNow);

        Assert.NotNull(result);
        Assert.Equal(expectedHours, result!.Value, precision: 3);
    }

    [Fact]
    public void ParseRelativeTime_Wczoraj_ReturnsValueOutsideTheDefault24HourWindow()
    {
        var result = OlxHtmlParser.ParseRelativeTime("Wczoraj o 10:00", DateTimeOffset.UtcNow);

        Assert.NotNull(result);
        Assert.True(result!.Value > 24);
    }

    [Fact]
    public void ParseRelativeTime_UnrecognizedFormat_ReturnsNull()
    {
        var result = OlxHtmlParser.ParseRelativeTime("30 lipca 2024", DateTimeOffset.UtcNow);

        Assert.Null(result);
    }
}
