namespace Estoque.EventListeners.Messages.Consumidos;

public sealed record ItemBaixaEstoque
{
    public Guid ProdutoId { get; init; }
    public int Quantidade { get; init; }
}
