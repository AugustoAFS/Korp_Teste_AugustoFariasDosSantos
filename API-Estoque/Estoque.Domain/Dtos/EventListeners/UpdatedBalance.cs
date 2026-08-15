namespace Estoque.Domain.Dtos.EventListeners;

public sealed record UpdatedBalance
{
    public Guid ProductId { get; init; }
    public int NewBalance { get; init; }
}
