using Estoque.Domain.Dtos.EventListeners;

namespace Estoque.ApplicationService.Interfaces;

public interface IStockDebitService
{
    Task<DebitResult> DebitStock(long notaFiscalId, Guid processamentoId, long? usuarioId, IReadOnlyList<DebitItem> itens, CancellationToken ct);
}
