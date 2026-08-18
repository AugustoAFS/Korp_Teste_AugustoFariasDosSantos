using Faturamento.Domain.Dtos;
using Faturamento.Domain.Dtos.Ai;
using Faturamento.Domain.Dtos.Request;

namespace Faturamento.ApplicationService.Interfaces;

public interface IInvoiceDraftService
{
    Task<Result<InterpretationResult>> InterpretItems(
        long invoiceId, InterpretItemsRequest request, CancellationToken ct);
}
