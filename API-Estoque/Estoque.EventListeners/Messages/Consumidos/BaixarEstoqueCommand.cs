using MassTransit;

namespace Estoque.EventListeners.Messages.Consumidos
{
    /// <summary>
    /// Publicado pelo Faturamento ao imprimir a nota.
    /// O urn precisa ser idêntico ao declarado do outro lado — se divergir,
    /// a mensagem entra na fila e fica parada sem consumidor.
    /// </summary>
    [MessageUrn("urn:message:emissor:baixar-estoque")]
    public sealed record BaixarEstoqueCommand(
        Guid NotaFiscalId,
        Guid ProcessamentoId,
        Guid? UsuarioId,
        IReadOnlyList<ItemBaixaEstoque> Itens);

    public sealed record ItemBaixaEstoque(Guid ProdutoId, int Quantidade);
}
