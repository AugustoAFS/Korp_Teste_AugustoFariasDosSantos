namespace Faturamento.Domain.Dtos.Ai;

public sealed record ParsedItem
{
    public required string Code { get; init; }
    public required int Quantity { get; init; }
}
