using MassTransit;

namespace Estoque.EventListeners.Messages.Publicados
{
    [MessageUrn("urn:message:emissor:estoque-rejeitado")]
    public sealed record EstoqueRejeitadoEvent(
        Guid NotaFiscalId,
        Guid ProcessamentoId,
        Guid ProdutoId,
        string Motivo);
}
