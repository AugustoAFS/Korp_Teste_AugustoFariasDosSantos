using MassTransit;

namespace Estoque.EventListeners.Messages.Publicados;

[MessageUrn("emissor:estoque-baixado")]
public sealed record EstoqueBaixadoEvent
{
    public long NotaFiscalId { get; init; }
    public Guid ProcessamentoId { get; init; }
    public IReadOnlyList<ItemBaixado> Itens { get; init; } = [];
}
