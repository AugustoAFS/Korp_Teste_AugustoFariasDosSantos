using MassTransit;

namespace Estoque.EventListeners.Messages.Publicados
{
    [MessageUrn("urn:message:emissor:produto-atualizado")]
    public sealed record ProdutoAtualizadoEvent(
        Guid ProdutoId,
        string Codigo,
        string Descricao,
        bool Ativo,
        DateTimeOffset AtualizadoEm);
}
