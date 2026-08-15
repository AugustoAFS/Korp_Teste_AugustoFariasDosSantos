using Faturamento.Domain.Dtos.EventListeners;

namespace Faturamento.ApplicationService.Interfaces;

public interface IFaturamentoEventPublisher
{
    Task PublishDebitStock(
        long notaFiscalId,
        Guid processamentoId,
        long? usuarioId,
        IReadOnlyList<DebitItem> itens,
        CancellationToken ct);
}
