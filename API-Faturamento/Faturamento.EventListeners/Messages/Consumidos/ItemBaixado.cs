namespace Faturamento.EventListeners.Messages.Consumidos;

public sealed record ItemBaixado
{
    public Guid ProdutoId { get; init; }
    public int SaldoNovo { get; init; }
}
