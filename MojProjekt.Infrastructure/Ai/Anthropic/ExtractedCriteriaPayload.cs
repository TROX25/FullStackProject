using System.Text.Json.Serialization;

namespace MojProjekt.Infrastructure.Ai.Anthropic;

/// <summary>Raw shape of the extract_search_criteria tool's "input" as returned by Claude, before validation/clamping.</summary>
public sealed class ExtractedCriteriaPayload
{
    [JsonPropertyName("intentSummary")]
    public string? IntentSummary { get; init; }

    [JsonPropertyName("priceMin")]
    public decimal? PriceMin { get; init; }

    [JsonPropertyName("priceMax")]
    public decimal? PriceMax { get; init; }

    [JsonPropertyName("yearMin")]
    public int? YearMin { get; init; }

    [JsonPropertyName("yearMax")]
    public int? YearMax { get; init; }

    [JsonPropertyName("mileageMax")]
    public int? MileageMax { get; init; }

    [JsonPropertyName("transmission")]
    public string? Transmission { get; init; }

    [JsonPropertyName("transmissionRequired")]
    public bool TransmissionRequired { get; init; }

    [JsonPropertyName("fuelType")]
    public string? FuelType { get; init; }

    [JsonPropertyName("fuelTypeRequired")]
    public bool FuelTypeRequired { get; init; }

    [JsonPropertyName("bodyType")]
    public string? BodyType { get; init; }

    [JsonPropertyName("bodyTypeRequired")]
    public bool BodyTypeRequired { get; init; }

    [JsonPropertyName("brand")]
    public string? Brand { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; init; }
}
