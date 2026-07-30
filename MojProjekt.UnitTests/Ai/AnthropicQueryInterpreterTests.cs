using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MojProjekt.Domain.Listings;
using MojProjekt.Infrastructure.Ai.Anthropic;

namespace MojProjekt.UnitTests.Ai;

public class AnthropicQueryInterpreterTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private static AnthropicQueryInterpreter CreateSut(Func<HttpRequestMessage, HttpResponseMessage> respond, bool configured = true)
    {
        var httpClient = new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("https://api.anthropic.test") };
        var options = Options.Create(new AnthropicOptions { ApiKey = configured ? "test-key" : "" });
        return new AnthropicQueryInterpreter(httpClient, options, NullLogger<AnthropicQueryInterpreter>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task InterpretAsync_NoApiKeyConfigured_UsesFallbackWithoutCallingHttp()
    {
        var called = false;
        var sut = CreateSut(_ => { called = true; return JsonResponse("{}"); }, configured: false);

        var result = await sut.InterpretAsync("automatic diesel under 50000", CancellationToken.None);

        Assert.False(called);
        Assert.True(result.UsedFallbackExtraction);
        Assert.Equal(50000m, result.Criteria.PriceMax);
    }

    [Fact]
    public async Task InterpretAsync_ValidToolUseResponse_ReturnsMappedCriteria()
    {
        const string body = """
        {
          "content": [
            {
              "type": "tool_use",
              "name": "extract_search_criteria",
              "input": {
                "intentSummary": "Looking for an automatic estate under 60000 PLN.",
                "priceMax": 60000,
                "yearMin": 2019,
                "transmission": "Automatic",
                "transmissionRequired": false,
                "bodyType": "Estate"
              }
            }
          ]
        }
        """;

        var sut = CreateSut(_ => JsonResponse(body));

        var result = await sut.InterpretAsync("a reliable family estate under PLN 60,000", CancellationToken.None);

        Assert.False(result.UsedFallbackExtraction);
        Assert.Equal(60000m, result.Criteria.PriceMax);
        Assert.Equal(2019, result.Criteria.YearMin);
        Assert.Equal(Transmission.Automatic, result.Criteria.TransmissionPreference!.Value);
        Assert.Equal("Looking for an automatic estate under 60000 PLN.", result.IntentSummary);
    }

    [Fact]
    public async Task InterpretAsync_ToolNotInvoked_RetriesThenFallsBack()
    {
        const string textOnlyBody = """{ "content": [ { "type": "text", "text": "I cannot help with that." } ] }""";

        var callCount = 0;
        var sut = CreateSut(_ => { callCount++; return JsonResponse(textOnlyBody); });

        var result = await sut.InterpretAsync("something vague", CancellationToken.None);

        Assert.Equal(2, callCount); // one initial attempt + one retry
        Assert.True(result.UsedFallbackExtraction);
    }

    [Fact]
    public async Task InterpretAsync_HttpErrorFromAnthropic_FallsBackGracefullyWithoutThrowing()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await sut.InterpretAsync("diesel automatic", CancellationToken.None);

        Assert.True(result.UsedFallbackExtraction);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task InterpretAsync_MalformedToolInput_FallsBackGracefullyWithoutThrowing()
    {
        const string malformedBody = """
        {
          "content": [
            { "type": "tool_use", "name": "extract_search_criteria", "input": { "yearMin": "not-a-number" } }
          ]
        }
        """;

        var sut = CreateSut(_ => JsonResponse(malformedBody));

        var result = await sut.InterpretAsync("anything", CancellationToken.None);

        Assert.True(result.UsedFallbackExtraction);
    }
}
