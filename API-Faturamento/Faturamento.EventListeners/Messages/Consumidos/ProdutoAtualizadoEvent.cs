using MassTransit;

namespace Faturamento.EventListeners.Messages.Consumidos;

[MessageUrn("emissor:produto-atualizado")]
public sealed record ProdutoAtualizadoEvent
{
    public Guid ProdutoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public DateTimeOffset AtualizadoEm { get; init; }
}
