using Estoque.Domain.Dtos.EventListeners;

namespace Estoque.ApplicationService.Interfaces;

public interface IEstoqueEventPublisher
{
    Task<StoredEvent> PublishProductCreated(Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct);

    Task<StoredEvent> PublishProductUpdated(Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct);

    Task<StoredEvent> PublishStockDebited(long notaFiscalId, Guid processamentoId, IReadOnlyList<UpdatedBalance> itens, CancellationToken ct);

    Task<StoredEvent> PublishStockRejected(long notaFiscalId, Guid processamentoId, Guid produtoId, string motivo, CancellationToken ct);

    Task Republish(StoredEvent evento, CancellationToken ct);
}
