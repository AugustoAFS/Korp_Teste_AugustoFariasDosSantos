namespace Faturamento.Domain.Dtos.Ai;

public sealed record CatalogEntry
{
    public required string Code { get; init; }
    public required string Description { get; init; }
}
