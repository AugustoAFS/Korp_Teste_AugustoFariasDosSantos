using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Entities;

namespace Faturamento.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<(IReadOnlyList<Invoice> Items, int Total)> GetPaged(
        InvoiceFilterRequest filter, long? onlyUserId, CancellationToken ct);

    Task<Invoice?> GetById(long id, CancellationToken ct);

    Task<Invoice?> GetByProcessing(long id, Guid processingId, CancellationToken ct);

    Task<IReadOnlyList<Invoice>> Expired(TimeSpan timeout, int limit, CancellationToken ct);

    Task<long> NextNumber(CancellationToken ct);

    Task Add(Invoice invoice, CancellationToken ct);

    Task<bool> StartPrinting(long id, Guid processingId, CancellationToken ct);

    Task<bool> RestartPrinting(long id, Guid processingId, CancellationToken ct);
}
