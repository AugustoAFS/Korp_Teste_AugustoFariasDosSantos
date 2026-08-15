using Estoque.Domain.Dtos.EventListeners;

namespace Estoque.ApplicationService.Interfaces;

public interface IEstoqueEventPublisher
{
    Task PublishProductCreated(Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct);

    Task PublishProductUpdated(Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct);

    Task PublishStockDebited(long notaFiscalId, Guid processamentoId, IReadOnlyList<UpdatedBalance> itens, CancellationToken ct);

    Task PublishStockRejected(long notaFiscalId, Guid processamentoId, Guid produtoId, string motivo, CancellationToken ct);
}
