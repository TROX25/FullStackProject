using MojProjekt.Application.Common;
using MojProjekt.Application.Search;
using MojProjekt.Domain.Listings;
using MojProjekt.Infrastructure.Search;

namespace MojProjekt.UnitTests.Search;

public class ListingRankingServiceTests
{
    private readonly ListingRankingService _sut = new();

    private static Listing MakeListing(
        decimal price = 50_000,
        int year = 2020,
        int? mileage = 80_000,
        Transmission transmission = Transmission.Manual,
        FuelType fuelType = FuelType.Petrol,
        BodyType bodyType = BodyType.Estate,
        string brand = "Skoda",
        string title = "Skoda Octavia 2020 Estate",
        string? description = "Reliable family car, well maintained.") => new()
    {
        Source = ListingSource.Sample,
        SourceListingId = Guid.NewGuid().ToString(),
        SourceUrl = "https://example.test/listing",
        Title = title,
        Description = description,
        Price = new Money(price, Currency.Pln),
        Year = year,
        Mileage = mileage,
        Transmission = transmission,
        FuelType = fuelType,
        BodyType = bodyType,
        Brand = brand,
        Model = "Octavia",
        City = "Warszawa",
        PublishedAt = DateTimeOffset.UtcNow,
        CrawledAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public void Rank_ListingWithinBudget_ScoresHigherThanOverBudgetListing()
    {
        var withinBudget = MakeListing(price: 45_000);
        var overBudget = MakeListing(price: 90_000);
        var criteria = SearchCriteria.Empty with { PriceMax = 60_000 };

        var results = _sut.Rank([withinBudget, overBudget], criteria, maxResults: 10);

        var withinScore = results.Single(r => r.Listing == withinBudget).Score;
        var overScore = results.Single(r => r.Listing == overBudget).Score;
        Assert.True(withinScore > overScore);
        Assert.Contains(results.Single(r => r.Listing == withinBudget).MatchReasons, r => r.Contains("budget"));
        Assert.Contains(results.Single(r => r.Listing == overBudget).UnmetPreferences, r => r.Contains("Over your budget"));
    }

    [Fact]
    public void Rank_YearAtOrAboveMinimum_AddsMatchReason()
    {
        var listing = MakeListing(year: 2019);
        var criteria = SearchCriteria.Empty with { YearMin = 2019 };

        var result = _sut.Rank([listing], criteria, maxResults: 10).Single();

        Assert.Contains(result.MatchReasons, r => r.Contains("meets your 'no older than"));
        Assert.Empty(result.UnmetPreferences);
    }

    [Fact]
    public void Rank_YearBelowMinimum_AddsUnmetPreferenceAndPenalizesScore()
    {
        var older = MakeListing(year: 2015);
        var newer = MakeListing(year: 2021);
        var criteria = SearchCriteria.Empty with { YearMin = 2019 };

        var results = _sut.Rank([older, newer], criteria, maxResults: 10);

        var olderResult = results.Single(r => r.Listing == older);
        Assert.Contains(olderResult.UnmetPreferences, u => u.Contains("older than your"));
        Assert.True(olderResult.Score < results.Single(r => r.Listing == newer).Score);
    }

    [Fact]
    public void Rank_SoftTransmissionPreferenceMismatch_KeepsListingButAddsUnmetPreference()
    {
        var manual = MakeListing(transmission: Transmission.Manual);
        var criteria = SearchCriteria.Empty with
        {
            TransmissionPreference = new EnumPreference<Transmission>(Transmission.Automatic, IsRequired: false)
        };

        var result = _sut.Rank([manual], criteria, maxResults: 10).Single();

        Assert.Contains(result.UnmetPreferences, u => u.Contains("preferred Automatic"));
    }

    [Fact]
    public void Rank_RequiredTransmissionMismatch_ExcludesListingEntirely()
    {
        var manual = MakeListing(transmission: Transmission.Manual);
        var automatic = MakeListing(transmission: Transmission.Automatic);
        var criteria = SearchCriteria.Empty with
        {
            TransmissionPreference = new EnumPreference<Transmission>(Transmission.Automatic, IsRequired: true)
        };

        var results = _sut.Rank([manual, automatic], criteria, maxResults: 10);

        Assert.DoesNotContain(results, r => r.Listing == manual);
        Assert.Contains(results, r => r.Listing == automatic);
    }

    [Fact]
    public void Rank_MatchingBrand_AddsMatchReason()
    {
        var bmw = MakeListing(brand: "BMW");
        var skoda = MakeListing(brand: "Skoda");
        var criteria = SearchCriteria.Empty with { Brand = "BMW" };

        var results = _sut.Rank([bmw, skoda], criteria, maxResults: 10);

        Assert.Contains(results.Single(r => r.Listing == bmw).MatchReasons, r => r.Contains("Brand matches"));
        Assert.Contains(results.Single(r => r.Listing == skoda).UnmetPreferences, r => r.Contains("does not match your requested brand"));
    }

    [Fact]
    public void Rank_KeywordAppearsInTitleOrDescription_IncreasesScoreAndAddsReason()
    {
        var matching = MakeListing(description: "A very reliable family estate, well cared for.");
        var nonMatching = MakeListing(description: "Sporty coupe for weekend drives.");
        var criteria = SearchCriteria.Empty with { Keywords = ["reliable", "family"] };

        var results = _sut.Rank([matching, nonMatching], criteria, maxResults: 10);

        var matchingResult = results.Single(r => r.Listing == matching);
        Assert.Contains(matchingResult.MatchReasons, r => r.Contains("Listing text mentions"));
        Assert.True(matchingResult.Score > results.Single(r => r.Listing == nonMatching).Score);
    }

    [Fact]
    public void Rank_ResultsAreOrderedByScoreDescending()
    {
        var best = MakeListing(price: 30_000, year: 2022);
        var worst = MakeListing(price: 120_000, year: 2010);
        var criteria = SearchCriteria.Empty with { PriceMax = 50_000, YearMin = 2020 };

        var results = _sut.Rank([worst, best], criteria, maxResults: 10);

        Assert.Equal(best, results.First().Listing);
        Assert.Equal(worst, results.Last().Listing);
    }

    [Fact]
    public void Rank_RespectsMaxResults()
    {
        var listings = Enumerable.Range(0, 5).Select(_ => MakeListing()).ToList();

        var results = _sut.Rank(listings, SearchCriteria.Empty, maxResults: 2);

        Assert.Equal(2, results.Count);
    }
}
