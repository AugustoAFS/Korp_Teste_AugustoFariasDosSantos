using MassTransit;

namespace Estoque.EventListeners.Messages.Publicados;

[MessageUrn("emissor:produto-criado")]
public sealed record ProdutoCriadoEvent
{
    public Guid ProdutoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public DateTimeOffset AtualizadoEm { get; init; }
}
