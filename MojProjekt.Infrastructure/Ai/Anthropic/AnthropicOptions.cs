namespace MojProjekt.Infrastructure.Ai.Anthropic;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    /// <summary>Read from appsettings/user-secrets/ANTHROPIC_API_KEY env var. Never hardcode or log this.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "claude-haiku-4-5-20251001";

    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
