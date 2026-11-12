using Microsoft.Extensions.AI;

namespace LzDetailingStore.UnitTests.Recommendations;

/// <summary>
/// In-memory <see cref="IChatClient"/> that returns canned responses. Never calls a real model.
/// </summary>
public sealed class FakeChatClient(Func<FakeChatClient.Call, ChatResponse> responder) : IChatClient
{
    public sealed record Call(int Index, string SystemPrompt, string UserPrompt, ChatOptions? Options);

    public List<Call> Calls { get; } = [];

    public static ChatResponse Json(string json, ChatFinishReason? finishReason = null) =>
        new(new ChatMessage(ChatRole.Assistant, json)) { FinishReason = finishReason ?? ChatFinishReason.Stop };

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        var call = new Call(
            Calls.Count,
            string.Join("\n", list.Where(m => m.Role == ChatRole.System).Select(m => m.Text)),
            string.Join("\n", list.Where(m => m.Role == ChatRole.User).Select(m => m.Text)),
            options);
        Calls.Add(call);
        return Task.FromResult(responder(call));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
