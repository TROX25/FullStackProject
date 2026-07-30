using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MojProjekt.Application.Listings;
using MojProjekt.Application.Crawling;
using MojProjekt.Application.Search;
using MojProjekt.Infrastructure.Ai;
using MojProjekt.Infrastructure.Ai.Anthropic;
using MojProjekt.Infrastructure.Crawling;
using MojProjekt.Infrastructure.Crawling.Fallback;
using MojProjekt.Infrastructure.Crawling.Olx;
using MojProjekt.Infrastructure.Persistence;
using MojProjekt.Infrastructure.Search;

namespace MojProjekt.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection configuration.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<ICrawlRunRepository, CrawlRunRepository>();

        services.Configure<CrawlerOptions>(configuration.GetSection(CrawlerOptions.SectionName));
        services.Configure<OlxCrawlerOptions>(configuration.GetSection(OlxCrawlerOptions.SectionName));

        services.AddHttpClient("Olx", (sp, client) =>
        {
            var olxOptions = sp.GetRequiredService<IOptions<OlxCrawlerOptions>>().Value;
            client.BaseAddress = new Uri(olxOptions.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(olxOptions.UserAgent);
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddKeyedScoped<IListingCrawler>("live", (sp, _) => new OlxCrawler(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Olx"),
            sp.GetRequiredService<IOptions<OlxCrawlerOptions>>(),
            sp.GetRequiredService<ILogger<OlxCrawler>>()));
        services.AddKeyedScoped<IListingCrawler, SampleDataCrawler>("sample");
        services.AddScoped<IListingCrawler, CompositeListingCrawler>();
        services.AddSingleton<ICrawlRunner, CrawlRunner>();

        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        services.PostConfigure<AnthropicOptions>(o =>
        {
            if (string.IsNullOrWhiteSpace(o.ApiKey))
            {
                o.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;
            }
        });

        services.AddHttpClient("Anthropic", (sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<AnthropicOptions>>().Value.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<INaturalLanguageQueryInterpreter>(sp => new AnthropicQueryInterpreter(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Anthropic"),
            sp.GetRequiredService<IOptions<AnthropicOptions>>(),
            sp.GetRequiredService<ILogger<AnthropicQueryInterpreter>>()));
        services.AddSingleton<IListingRankingService, ListingRankingService>();

        return services;
    }
}
