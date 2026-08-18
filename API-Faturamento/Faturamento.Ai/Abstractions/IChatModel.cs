namespace Faturamento.Ai.Abstractions;

public interface IChatModel
{
    bool Enabled { get; }

    Task<string?> Complete(ChatRequest request, CancellationToken ct);
}
