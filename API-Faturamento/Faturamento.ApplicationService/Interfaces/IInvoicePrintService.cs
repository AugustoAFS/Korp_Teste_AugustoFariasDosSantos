using Faturamento.Domain.Dtos;
using Faturamento.Domain.Dtos.Response;

namespace Faturamento.ApplicationService.Interfaces;

public interface IInvoicePrintService
{
    Task<Result<InvoiceResponse>> PrintInvoice(long id, CancellationToken ct);

    Task CloseInvoice(Guid messageId, long invoiceId, Guid processingId, CancellationToken ct);

    Task RejectInvoice(Guid messageId, long invoiceId, Guid processingId, string reason, CancellationToken ct);

    Task<int> ExpirePrintings(TimeSpan timeout, int limit, CancellationToken ct);
}
