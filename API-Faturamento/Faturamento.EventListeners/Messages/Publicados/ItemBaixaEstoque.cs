namespace Faturamento.EventListeners.Messages.Publicados;

public sealed record ItemBaixaEstoque
{
    public Guid ProdutoId { get; init; }
    public int Quantidade { get; init; }
}
