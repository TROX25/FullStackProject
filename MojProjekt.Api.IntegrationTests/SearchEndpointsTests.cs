using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MojProjekt.Api.IntegrationTests;

public class SearchEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    // The factory seeds one crawl in IAsyncLifetime.InitializeAsync before any test runs, so these
    // tests can search immediately without each paying for their own crawl+upsert cycle.
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Search_AssignmentExampleQuery_ReturnsRankedResultsWithMatchReasons()
    {
        var response = await _client.PostAsJsonAsync("/api/search", new
        {
            query = "a reliable family estate under PLN 60,000, preferably automatic and no older than 2019",
            maxResults = 10
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var results = body.GetProperty("results");
        Assert.True(results.GetArrayLength() > 0);

        var scores = results.EnumerateArray().Select(r => r.GetProperty("score").GetInt32()).ToList();
        Assert.Equal(scores.OrderByDescending(s => s), scores);

        var firstResult = results[0];
        Assert.True(firstResult.GetProperty("matchReasons").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/search", new { query = "", maxResults = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithoutAnthropicKeyConfigured_StillReturnsResultsViaFallback()
    {
        var response = await _client.PostAsJsonAsync("/api/search", new { query = "cheap automatic car", maxResults = 5 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var warnings = body.GetProperty("warnings").EnumerateArray().Select(w => w.GetString()).ToList();
        Assert.Contains(warnings, w => w != null && w.Contains("AI query interpretation"));
    }
}
