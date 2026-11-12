using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.AI;

namespace LzDetailingStore.Infrastructure.AI;

/// <summary>
/// Applies Anthropic-specific request defaults through <see cref="ChatOptions.RawRepresentationFactory"/>
/// so the Application layer stays provider-agnostic:
/// top-level prompt caching (the catalog system prompt repeats across batches) and, optionally,
/// the server-side refusal fallback (<c>fallbacks: "default"</c>, beta <c>server-side-fallback-2026-07-01</c>).
/// </summary>
internal sealed class AnthropicRequestDefaultsChatClient(
    IChatClient innerClient,
    string defaultModel,
    int defaultMaxOutputTokens,
    bool enableServerSideFallback)
    : DelegatingChatClient(innerClient)
{
    private const string ServerSideFallbackBeta = "server-side-fallback-2026-07-01";

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, WithDefaults(options), cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(messages, WithDefaults(options), cancellationToken);

    private ChatOptions WithDefaults(ChatOptions? options)
    {
        var result = options?.Clone() ?? new ChatOptions();
        if (result.RawRepresentationFactory is not null)
            return result; // Caller supplied its own raw parameters; leave them untouched.

        result.RawRepresentationFactory = _ =>
        {
            var parameters = new MessageCreateParams
            {
                // The adapter keeps raw values as-is, so model and max_tokens must be real values here.
                Model = result.ModelId ?? defaultModel,
                MaxTokens = result.MaxOutputTokens ?? defaultMaxOutputTokens,
                Messages = [],
                CacheControl = new BetaCacheControlEphemeral()
            };

            if (enableServerSideFallback)
            {
                parameters = parameters with
                {
                    Fallbacks = new Default(),
                    Betas = [ServerSideFallbackBeta]
                };
            }

            return parameters;
        };

        return result;
    }
}
