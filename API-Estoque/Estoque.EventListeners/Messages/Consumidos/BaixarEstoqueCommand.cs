using MassTransit;

namespace Estoque.EventListeners.Messages.Consumidos;

[MessageUrn("emissor:baixar-estoque")]
public sealed record BaixarEstoqueCommand
{
    public long NotaFiscalId { get; init; }
    public Guid ProcessamentoId { get; init; }
    public long? UsuarioId { get; init; }
    public IReadOnlyList<ItemBaixaEstoque> Itens { get; init; } = [];
}
