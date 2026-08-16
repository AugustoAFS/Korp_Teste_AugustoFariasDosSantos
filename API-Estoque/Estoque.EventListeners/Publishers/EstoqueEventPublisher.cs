using System.Text.Json;
using Estoque.ApplicationService.Interfaces;
using Estoque.Domain.Dtos.EventListeners;
using Estoque.Domain.Interfaces;
using Estoque.EventListeners.Messages.Publicados;

namespace Estoque.EventListeners.Publishers;

public sealed class EstoqueEventPublisher(IOutboxRepository outbox) : IEstoqueEventPublisher
{
    public Task<StoredEvent> PublishProductCreated(
        Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct)
        => Store(
            new ProdutoCriadoEvent
            {
                ProdutoId = produtoId,
                Codigo = codigo,
                Descricao = descricao,
                Ativo = ativo,
                AtualizadoEm = DateTimeOffset.UtcNow
            },
            ct);

    public Task<StoredEvent> PublishProductUpdated(
        Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct)
        => Store(
            new ProdutoAtualizadoEvent
            {
                ProdutoId = produtoId,
                Codigo = codigo,
                Descricao = descricao,
                Ativo = ativo,
                AtualizadoEm = DateTimeOffset.UtcNow
            },
            ct);

    public Task<StoredEvent> PublishStockDebited(
        long notaFiscalId,
        Guid processamentoId,
        IReadOnlyList<UpdatedBalance> itens,
        CancellationToken ct)
        => Store(
            new EstoqueBaixadoEvent
            {
                NotaFiscalId = notaFiscalId,
                ProcessamentoId = processamentoId,
                Itens = [.. itens.Select(item => new ItemBaixado
                {
                    ProdutoId = item.ProductId,
                    SaldoNovo = item.NewBalance
                })]
            },
            ct);

    public Task<StoredEvent> PublishStockRejected(
        long notaFiscalId, Guid processamentoId, Guid produtoId, string motivo, CancellationToken ct)
        => Store(
            new EstoqueRejeitadoEvent
            {
                NotaFiscalId = notaFiscalId,
                ProcessamentoId = processamentoId,
                ProdutoId = produtoId,
                Motivo = motivo
            },
            ct);

    public Task Republish(StoredEvent evento, CancellationToken ct)
        => outbox.Add(evento.Type, evento.Payload, ct);

    private async Task<StoredEvent> Store<T>(T evento, CancellationToken ct) where T : notnull
    {
        var armazenado = new StoredEvent
        {
            Type = typeof(T).Name,
            Payload = JsonSerializer.Serialize(evento)
        };

        await outbox.Add(armazenado.Type, armazenado.Payload, ct);

        return armazenado;
    }
}
