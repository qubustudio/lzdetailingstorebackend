using Anthropic;
using Microsoft.Extensions.AI;

namespace LzDetailingStore.Infrastructure.AI;

/// <summary>
/// Builds the <see cref="IChatClient"/> backed by the official Anthropic SDK (beta Messages endpoint,
/// required for server-side fallbacks). Thinking, temperature and sampling are intentionally left at the API defaults.
/// </summary>
public static class AnthropicChatClientFactory
{
    public static IChatClient Create(AnthropicOptions options, HttpClient? httpClient = null)
    {
        var client = httpClient is null
            ? new AnthropicClient { ApiKey = options.ApiKey }
            : new AnthropicClient { ApiKey = options.ApiKey, HttpClient = httpClient };

        var model = string.IsNullOrWhiteSpace(options.Model) ? AnthropicOptions.DefaultModel : options.Model;
        var maxTokens = options.MaxOutputTokens > 0 ? options.MaxOutputTokens : AnthropicOptions.DefaultMaxOutputTokens;

        IChatClient inner = client.Beta.AsIChatClient(model, maxTokens);
        return new AnthropicRequestDefaultsChatClient(inner, model, maxTokens, options.EnableServerSideFallback);
    }
}
