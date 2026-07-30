using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using MojProjekt.Domain.Listings;

namespace MojProjekt.Infrastructure.Crawling.Olx;

/// <summary>
/// Pure HTML -> data parsing, deliberately kept free of HTTP concerns so it can be unit-tested
/// against saved fixture HTML with no network access (see OlxHtmlParserTests).
///
/// IMPORTANT: the CSS selectors below are a best-effort based on OLX's historically documented
/// markup conventions (data-cy/data-testid attributes). This sandboxed build environment has no
/// general internet access (outbound HTTPS to olx.pl is blocked by the environment's network
/// policy), so these selectors could NOT be verified against live OLX HTML during development.
/// Before relying on the live crawl path, run it once locally against the real site and adjust the
/// selectors below if OLX's markup has changed — see ASSUMPTIONS.md.
/// </summary>
public static class OlxHtmlParser
{
    private static readonly IBrowsingContext HtmlContext = AngleSharp.BrowsingContext.New(Configuration.Default);

    private static readonly Regex PriceDigitsRegex = new(@"[\d\s]+(?=[,.]?\d*\s*(zł|pln)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HoursRegex = new(@"(\d+)\s*godz", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MinutesRegex = new(@"(\d+)\s*min", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ClockTimeRegex = new(@"(\d{1,2}):(\d{2})", RegexOptions.Compiled);
    private static readonly Regex DigitsOnlyRegex = new(@"\d+", RegexOptions.Compiled);

    public static async Task<IReadOnlyList<OlxSearchResultCard>> ParseSearchResultsPageAsync(string html, string baseUrl)
    {
        var document = await HtmlContext.OpenAsync(req => req.Content(html));

        var cardElements = document.QuerySelectorAll("[data-cy='l-card']");
        if (cardElements.Length == 0)
        {
            // Fallback: any anchor pointing at a listing detail page, deduplicated by href.
            cardElements = document.QuerySelectorAll("a[href*='/oferta/'], a[href*='/d/oferta/']");
        }

        var results = new List<OlxSearchResultCard>();
        var seenUrls = new HashSet<string>();

        foreach (var card in cardElements)
        {
            var anchor = card.TagName.Equals("A", StringComparison.OrdinalIgnoreCase)
                ? card
                : card.QuerySelector("a[href*='/oferta/'], a[href*='/d/oferta/']");

            var href = anchor?.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            var detailUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : new Uri(new Uri(baseUrl), href).ToString();
            if (!seenUrls.Add(detailUrl))
            {
                continue;
            }

            var title = card.QuerySelector("h4, h6, [data-cy='ad-card-title']")?.TextContent.Trim()
                ?? anchor?.GetAttribute("title")?.Trim()
                ?? card.TextContent.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var priceText = card.QuerySelector("[data-testid='ad-price']")?.TextContent;
            var price = ParsePrice(priceText);

            var locationDateText = card.QuerySelector("[data-testid='location-date']")?.TextContent ?? string.Empty;
            var (city, hoursAgo) = ParseLocationAndTime(locationDateText, DateTimeOffset.UtcNow);

            var thumbnailUrl = card.QuerySelector("img")?.GetAttribute("src");

            results.Add(new OlxSearchResultCard(
                SourceListingId: ExtractListingId(detailUrl),
                Title: title,
                PriceAmount: price,
                City: city,
                HoursAgo: hoursAgo,
                DetailUrl: detailUrl,
                ThumbnailUrl: thumbnailUrl));
        }

        return results;
    }

    public static async Task<(string? Description, int? Year, int? Mileage, Transmission Transmission,
            FuelType FuelType, BodyType BodyType, string Brand, string Model, IReadOnlyList<string> ImageUrls)>
        ParseDetailPageAsync(string html)
    {
        var document = await HtmlContext.OpenAsync(req => req.Content(html));

        var description = document.QuerySelector("[data-cy='ad_description'], [data-testid='ad_description']")?.TextContent.Trim();

        var parameters = ExtractParameters(document);

        var year = ParseIntParameter(parameters, "Rok produkcji");
        var mileage = ParseIntParameter(parameters, "Przebieg");
        var transmission = ParseTransmission(parameters.GetValueOrDefault("Skrzynia biegów"));
        var fuelType = ParseFuelType(parameters.GetValueOrDefault("Paliwo"));
        var bodyType = ParseBodyType(parameters.GetValueOrDefault("Typ nadwozia"));
        var brand = parameters.GetValueOrDefault("Marka pojazdu") ?? "Unknown";
        var model = parameters.GetValueOrDefault("Model pojazdu") ?? "Unknown";

        var imageUrls = document.QuerySelectorAll("[data-testid='swiper-wrapper'] img, .swiper-slide img")
            .Select(img => img.GetAttribute("src"))
            .Where(src => !string.IsNullOrWhiteSpace(src))
            .Cast<string>()
            .Distinct()
            .Take(10)
            .ToList();

        return (description, year, mileage, transmission, fuelType, bodyType, brand, model, imageUrls);
    }

    private static Dictionary<string, string> ExtractParameters(IDocument document)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var container = document.QuerySelector("[data-testid='ad-parameters-container']");
        var items = container?.QuerySelectorAll("p, li") ?? document.QuerySelectorAll("[data-testid='ad-parameter']");

        foreach (var item in items)
        {
            var text = item.TextContent.Trim();
            var separatorIndex = text.IndexOf(':');
            if (separatorIndex > 0 && separatorIndex < text.Length - 1)
            {
                var key = text[..separatorIndex].Trim();
                var value = text[(separatorIndex + 1)..].Trim();
                parameters[key] = value;
            }
        }

        return parameters;
    }

    private static decimal? ParsePrice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var digits = new string(text.Where(c => char.IsDigit(c) || c is ',' or '.').ToArray())
            .Replace(" ", "");

        // OLX renders prices like "45 900 zł" (no decimal separator meaningfully used for whole PLN amounts).
        var normalized = Regex.Replace(text, @"[^\d]", "");
        return decimal.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static (string? City, double? HoursAgo) ParseLocationAndTime(string text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null);
        }

        var parts = text.Split('-', 2);
        var city = parts.Length > 0 ? parts[0].Trim() : null;
        var timePart = parts.Length > 1 ? parts[1].Trim() : text;

        return (city, ParseRelativeTime(timePart, now));
    }

    /// <summary>
    /// Handles OLX's Polish relative-time labels: "Dzisiaj o HH:MM", "Wczoraj o HH:MM",
    /// "X godzin temu", "X minut temu". An absolute calendar date (older listings) returns null,
    /// which the caller treats as outside the 24h window.
    /// </summary>
    public static double? ParseRelativeTime(string text, DateTimeOffset now)
    {
        var lower = text.ToLowerInvariant();

        var minutesMatch = MinutesRegex.Match(lower);
        if (minutesMatch.Success && int.TryParse(minutesMatch.Groups[1].Value, out var minutes))
        {
            return minutes / 60.0;
        }

        var hoursMatch = HoursRegex.Match(lower);
        if (hoursMatch.Success && int.TryParse(hoursMatch.Groups[1].Value, out var hours))
        {
            return hours;
        }

        if (lower.Contains("dzisiaj") || lower.Contains("today"))
        {
            var clockMatch = ClockTimeRegex.Match(lower);
            if (clockMatch.Success)
            {
                var listingTimeUtc = now.Date.AddHours(int.Parse(clockMatch.Groups[1].Value))
                    .AddMinutes(int.Parse(clockMatch.Groups[2].Value));
                return Math.Max(0, (now - new DateTimeOffset(listingTimeUtc, TimeSpan.Zero)).TotalHours);
            }

            return 1; // "Dzisiaj" with no clock time: assume recent.
        }

        if (lower.Contains("wczoraj") || lower.Contains("yesterday"))
        {
            return 25; // Just outside the 24h window by convention; caller filters on MaxAge anyway.
        }

        return null;
    }

    private static string ExtractListingId(string detailUrl)
    {
        var match = DigitsOnlyRegex.Matches(detailUrl).LastOrDefault();
        return match is not null && match.Value.Length >= 5 ? match.Value : detailUrl;
    }

    private static int? ParseIntParameter(Dictionary<string, string> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var raw))
        {
            return null;
        }

        var digits = DigitsOnlyRegex.Matches(raw).Select(m => m.Value);
        var joined = string.Concat(digits);
        return int.TryParse(joined, out var value) ? value : null;
    }

    private static Transmission ParseTransmission(string? raw) => raw?.ToLowerInvariant() switch
    {
        null => Transmission.Unknown,
        var s when s.Contains("automat") => Transmission.Automatic,
        var s when s.Contains("manual") => Transmission.Manual,
        _ => Transmission.Unknown
    };

    private static FuelType ParseFuelType(string? raw) => raw?.ToLowerInvariant() switch
    {
        null => FuelType.Unknown,
        var s when s.Contains("diesel") => FuelType.Diesel,
        var s when s.Contains("benzyna") && s.Contains("lpg") => FuelType.Lpg,
        var s when s.Contains("benzyna") => FuelType.Petrol,
        var s when s.Contains("hybryda") || s.Contains("hybrydowy") => FuelType.Hybrid,
        var s when s.Contains("elektryczny") => FuelType.Electric,
        var s when s.Contains("lpg") || s.Contains("gaz") => FuelType.Lpg,
        _ => FuelType.Unknown
    };

    private static BodyType ParseBodyType(string? raw) => raw?.ToLowerInvariant() switch
    {
        null => BodyType.Unknown,
        var s when s.Contains("kombi") => BodyType.Estate,
        var s when s.Contains("suv") => BodyType.Suv,
        var s when s.Contains("sedan") => BodyType.Sedan,
        var s when s.Contains("hatchback") || s.Contains("kompakt") => BodyType.Hatchback,
        var s when s.Contains("coupe") || s.Contains("kupe") => BodyType.Coupe,
        var s when s.Contains("van") || s.Contains("minivan") => BodyType.Van,
        var s when s.Contains("pickup") => BodyType.Pickup,
        var s when s.Contains("kabriolet") || s.Contains("cabrio") => BodyType.Convertible,
        _ => BodyType.Unknown
    };
}
