using MassTransit;

namespace Estoque.EventListeners.Messages.Publicados
{
    [MessageUrn("urn:message:emissor:produto-criado")]
    public sealed record ProdutoCriadoEvent(
        Guid ProdutoId,
        string Codigo,
        string Descricao,
        bool Ativo,
        DateTimeOffset AtualizadoEm);
}
