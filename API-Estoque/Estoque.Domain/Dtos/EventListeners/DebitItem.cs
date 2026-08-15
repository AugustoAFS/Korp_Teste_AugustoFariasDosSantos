namespace Estoque.Domain.Dtos.EventListeners;

public sealed record DebitItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
