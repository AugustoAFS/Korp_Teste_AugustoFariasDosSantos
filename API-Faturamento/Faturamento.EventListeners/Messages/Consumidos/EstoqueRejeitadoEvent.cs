using MassTransit;

namespace Faturamento.EventListeners.Messages.Consumidos;

[MessageUrn("emissor:estoque-rejeitado")]
public sealed record EstoqueRejeitadoEvent
{
    public long NotaFiscalId { get; init; }
    public Guid ProcessamentoId { get; init; }
    public Guid ProdutoId { get; init; }
    public string Motivo { get; init; } = string.Empty;
}
