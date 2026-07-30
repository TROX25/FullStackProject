using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MojProjekt.Api.IntegrationTests;

/// <summary>
/// Boots the real Api host against a throwaway SQLite file and forces Crawler:Mode=SampleOnly, so
/// integration tests exercise the full DI graph/migrations/endpoints deterministically and offline
/// (no dependency on OLX or an Anthropic API key being reachable). Seeds the database with one crawl
/// on first use (see InitializeAsync) so individual tests don't each pay for their own crawl+upsert
/// of ~100 listings.
///
/// Overrides are applied via environment variables rather than WebApplicationFactory's
/// ConfigureAppConfiguration: Program.cs's top-level statements read configuration (e.g.
/// ConnectionStrings:DefaultConnection) eagerly, before WebApplicationBuilder.Build() runs, which is
/// also the point where the deferred test host applies ConfigureAppConfiguration overrides — too
/// late for those eager reads. Environment variables are already part of the process when Program.cs
/// executes (WebApplicationFactory runs it in-process via reflection), so ASP.NET Core's default
/// environment-variable configuration provider picks them up in time.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = CreateTestDbPath();

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"Data Source={_dbPath}");
        Environment.SetEnvironmentVariable("Crawler__Mode", "SampleOnly");
    }

    private static string CreateTestDbPath()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "test-dbs");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"mojprojekt-tests-{Guid.NewGuid():N}.db");
    }

    public async Task InitializeAsync()
    {
        var client = CreateClient();
        await client.PostAsync("/api/crawl", content: null);

        for (var attempt = 0; attempt < 60; attempt++)
        {
            var response = await client.GetAsync("/api/crawl/latest");
            if (response.IsSuccessStatusCode)
            {
                var run = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (run.GetProperty("status").GetString() is "Completed" or "Failed")
                {
                    return;
                }
            }

            await Task.Delay(300);
        }

        throw new TimeoutException("Seed crawl did not complete in time.");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
