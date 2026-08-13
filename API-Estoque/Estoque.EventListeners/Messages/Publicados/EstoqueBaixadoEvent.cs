using MassTransit;

namespace Estoque.EventListeners.Messages.Publicados
{
    [MessageUrn("urn:message:emissor:estoque-baixado")]
    public sealed record EstoqueBaixadoEvent(
        Guid NotaFiscalId,
        Guid ProcessamentoId,
        IReadOnlyList<ItemBaixado> Itens);

    public sealed record ItemBaixado(Guid ProdutoId, int SaldoNovo);
}
