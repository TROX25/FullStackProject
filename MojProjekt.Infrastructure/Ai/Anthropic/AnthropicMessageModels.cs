using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MojProjekt.Infrastructure.Ai.Anthropic;

public sealed class AnthropicRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; } = 1024;

    [JsonPropertyName("system")]
    public string? System { get; init; }

    [JsonPropertyName("messages")]
    public required List<AnthropicMessage> Messages { get; init; }

    [JsonPropertyName("tools")]
    public List<AnthropicTool>? Tools { get; init; }

    [JsonPropertyName("tool_choice")]
    public JsonObject? ToolChoice { get; init; }
}

public sealed class AnthropicMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

public sealed class AnthropicTool
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("input_schema")]
    public required JsonObject InputSchema { get; init; }
}

public sealed class AnthropicResponse
{
    [JsonPropertyName("content")]
    public List<AnthropicContentBlock> Content { get; init; } = [];

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; init; }
}

public sealed class AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("input")]
    public JsonObject? Input { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
