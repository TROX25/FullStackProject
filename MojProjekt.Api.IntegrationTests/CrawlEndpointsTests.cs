using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MojProjekt.Api.IntegrationTests;

public class CrawlEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task TriggerCrawl_ThenPollLatest_CompletesUsingSampleFallback()
    {
        var triggerResponse = await _client.PostAsync("/api/crawl", content: null);
        Assert.Equal(HttpStatusCode.Accepted, triggerResponse.StatusCode);

        var run = await PollUntilDoneAsync();

        Assert.Equal("Completed", run.GetProperty("status").GetString());
        Assert.Equal("Fallback", run.GetProperty("sourceUsed").GetString());
        Assert.True(run.GetProperty("listingsFound").GetInt32() > 0);
    }

    [Fact]
    public async Task GetListings_DatasetAlreadySeeded_ReturnsPagedResults()
    {
        // The factory seeds one crawl in IAsyncLifetime.InitializeAsync, so this test only exercises
        // the read path — no need to trigger another crawl.
        var response = await _client.GetAsync("/api/listings?page=1&pageSize=5");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("items").GetArrayLength() > 0);
        Assert.True(body.GetProperty("totalCount").GetInt32() > 0);
    }

    private async Task<JsonElement> PollUntilDoneAsync()
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var response = await _client.GetAsync("/api/crawl/latest");
            if (response.IsSuccessStatusCode)
            {
                var run = await response.Content.ReadFromJsonAsync<JsonElement>();
                var status = run.GetProperty("status").GetString();
                if (status is "Completed" or "Failed")
                {
                    return run;
                }
            }

            await Task.Delay(300);
        }

        throw new TimeoutException("Crawl run did not complete in time.");
    }
}
