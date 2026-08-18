namespace Faturamento.Domain.Dtos.Ai;

public sealed record InterpretationResult
{
    public required IReadOnlyList<InterpretedItem> Items { get; init; }
    public required IReadOnlyList<string> Unresolved { get; init; }
}
