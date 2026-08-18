using System.Text.Json;

namespace Faturamento.Ai.Abstractions;

public sealed record ChatRequest
{
    public required string Instruction { get; init; }
    public required string Prompt { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? JsonSchema { get; init; }
}
