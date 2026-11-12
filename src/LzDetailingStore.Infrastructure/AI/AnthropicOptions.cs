namespace LzDetailingStore.Infrastructure.AI;

/// <summary>
/// Configuration section "Anthropic". The API key must come from user-secrets or environment variables
/// (Anthropic__ApiKey or ANTHROPIC_API_KEY), never from appsettings files.
/// </summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    public const string DefaultModel = "claude-opus-5-5";
    public const int DefaultMaxOutputTokens = 16000;

    public string? ApiKey { get; set; }
    public string Model { get; set; } = DefaultModel;
    public int MaxOutputTokens { get; set; } = DefaultMaxOutputTokens;

    /// <summary>Server-side refusal fallback ("fallbacks": "default"). Enabled by default.</summary>
    public bool EnableServerSideFallback { get; set; } = true;
}
