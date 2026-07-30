using MojProjekt.Domain.Listings;
using MojProjekt.Infrastructure.Ai;

namespace MojProjekt.UnitTests.Ai;

public class NaiveCriteriaExtractorTests
{
    [Fact]
    public void Extract_PriceAndYearAndSoftTransmissionPreference_ParsesAssignmentExampleQuery()
    {
        var criteria = NaiveCriteriaExtractor.Extract(
            "a reliable family estate under PLN 60,000, preferably automatic and no older than 2019");

        Assert.Equal(60000m, criteria.PriceMax);
        Assert.Equal(2019, criteria.YearMin);
        Assert.NotNull(criteria.TransmissionPreference);
        Assert.Equal(Transmission.Automatic, criteria.TransmissionPreference!.Value);
        Assert.False(criteria.TransmissionPreference.IsRequired);
        Assert.NotNull(criteria.BodyTypePreference);
        Assert.Equal(BodyType.Estate, criteria.BodyTypePreference!.Value);
    }

    [Fact]
    public void Extract_MustBeAutomatic_IsMarkedRequired()
    {
        var criteria = NaiveCriteriaExtractor.Extract("must be automatic, diesel");

        Assert.NotNull(criteria.TransmissionPreference);
        Assert.True(criteria.TransmissionPreference!.IsRequired);
        Assert.Equal(FuelType.Diesel, criteria.FuelTypePreference!.Value);
    }

    [Fact]
    public void Extract_QueryWithNoRecognizableCriteria_ReturnsEmptyCriteriaWithoutThrowing()
    {
        var criteria = NaiveCriteriaExtractor.Extract("show me something nice");

        Assert.Null(criteria.PriceMax);
        Assert.Null(criteria.YearMin);
        Assert.Null(criteria.TransmissionPreference);
    }

    [Fact]
    public void Extract_EmptyQuery_DoesNotThrow()
    {
        var criteria = NaiveCriteriaExtractor.Extract(string.Empty);

        Assert.Empty(criteria.Keywords);
    }
}
