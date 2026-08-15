using MassTransit;

namespace Faturamento.EventListeners.Messages.Consumidos;

[MessageUrn("emissor:estoque-baixado")]
public sealed record EstoqueBaixadoEvent
{
    public long NotaFiscalId { get; init; }
    public Guid ProcessamentoId { get; init; }
    public IReadOnlyList<ItemBaixado> Itens { get; init; } = [];
}
