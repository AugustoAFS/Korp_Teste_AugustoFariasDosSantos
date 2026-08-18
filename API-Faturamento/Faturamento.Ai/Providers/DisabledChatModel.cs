using Faturamento.Ai.Abstractions;

namespace Faturamento.Ai.Providers;

public sealed class DisabledChatModel : IChatModel
{
    public const string Name = "none";

    public bool Enabled => false;

    public Task<string?> Complete(ChatRequest request, CancellationToken ct) => Task.FromResult<string?>(null);
}
