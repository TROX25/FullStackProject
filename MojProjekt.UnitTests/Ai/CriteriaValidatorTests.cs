using MojProjekt.Domain.Listings;
using MojProjekt.Infrastructure.Ai;
using MojProjekt.Infrastructure.Ai.Anthropic;

namespace MojProjekt.UnitTests.Ai;

public class CriteriaValidatorTests
{
    [Fact]
    public void Validate_WellFormedPayload_MapsAllFieldsWithoutWarnings()
    {
        var payload = new ExtractedCriteriaPayload
        {
            IntentSummary = "Family estate under budget.",
            PriceMax = 60000,
            YearMin = 2019,
            Transmission = "Automatic",
            TransmissionRequired = false,
            FuelType = "Diesel",
            BodyType = "Estate",
            Keywords = ["reliable", "family"]
        };

        var (criteria, warnings) = CriteriaValidator.Validate(payload);

        Assert.Empty(warnings);
        Assert.Equal(60000m, criteria.PriceMax);
        Assert.Equal(2019, criteria.YearMin);
        Assert.Equal(Transmission.Automatic, criteria.TransmissionPreference!.Value);
        Assert.Equal(FuelType.Diesel, criteria.FuelTypePreference!.Value);
        Assert.Equal(BodyType.Estate, criteria.BodyTypePreference!.Value);
        Assert.Equal(["reliable", "family"], criteria.Keywords);
    }

    [Fact]
    public void Validate_MissingOptionalFields_ProducesEmptyCriteriaWithoutError()
    {
        var payload = new ExtractedCriteriaPayload { IntentSummary = "Anything goes." };

        var (criteria, warnings) = CriteriaValidator.Validate(payload);

        Assert.Empty(warnings);
        Assert.Null(criteria.PriceMax);
        Assert.Null(criteria.TransmissionPreference);
    }

    [Fact]
    public void Validate_YearOutOfPlausibleRange_IsRejectedWithWarning()
    {
        var payload = new ExtractedCriteriaPayload { IntentSummary = "x", YearMin = 1800 };

        var (criteria, warnings) = CriteriaValidator.Validate(payload);

        Assert.Null(criteria.YearMin);
        Assert.Contains(warnings, w => w.Contains("yearMin"));
    }

    [Fact]
    public void Validate_NegativePrice_IsRejectedWithWarning()
    {
        var payload = new ExtractedCriteriaPayload { IntentSummary = "x", PriceMax = -100 };

        var (criteria, warnings) = CriteriaValidator.Validate(payload);

        Assert.Null(criteria.PriceMax);
        Assert.Contains(warnings, w => w.Contains("priceMax"));
    }

    [Fact]
    public void Validate_PriceMinGreaterThanPriceMax_DropsPriceMinWithWarning()
    {
        var payload = new ExtractedCriteriaPayload { IntentSummary = "x", PriceMin = 90000, PriceMax = 50000 };

        var (criteria, warnings) = CriteriaValidator.Validate(payload);

        Assert.Null(criteria.PriceMin);
        Assert.Equal(50000m, criteria.PriceMax);
        Assert.Contains(warnings, w => w.Contains("priceMin was greater than priceMax"));
    }

    [Fact]
    public void Validate_UnrecognizedEnumValue_IsIgnoredWithWarning()
    {
        var payload = new ExtractedCriteriaPayload { IntentSummary = "x", Transmission = "Warp Drive" };

        var (criteria, warnings) = CriteriaValidator.Validate(payload);

        Assert.Null(criteria.TransmissionPreference);
        Assert.Contains(warnings, w => w.Contains("transmission"));
    }

    [Fact]
    public void Validate_KeywordsListLongerThanLimit_IsTruncated()
    {
        var payload = new ExtractedCriteriaPayload
        {
            IntentSummary = "x",
            Keywords = Enumerable.Range(0, 30).Select(i => $"kw{i}").ToList()
        };

        var (criteria, _) = CriteriaValidator.Validate(payload);

        Assert.True(criteria.Keywords.Count <= 15);
    }
}
