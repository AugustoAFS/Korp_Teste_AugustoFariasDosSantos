using Faturamento.Domain.Dtos;
using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Dtos.Response;

namespace Faturamento.ApplicationService.Interfaces;

public interface IInvoiceService
{
    Task<Result<PagedResult<InvoiceResponse>>> GetInvoices(InvoiceFilterRequest filter, CancellationToken ct);

    Task<Result<InvoiceResponse>> GetInvoiceById(long id, CancellationToken ct);

    Task<Result<InvoicePdf>> GetInvoicePdf(long id, CancellationToken ct);

    Task<Result<InvoiceResponse>> CreateInvoice(CancellationToken ct);

    Task<Result> DeleteInvoice(long id, CancellationToken ct);

    Task<Result<InvoiceResponse>> AddInvoiceItem(long id, AddInvoiceItemRequest request, CancellationToken ct);

    Task<Result<InvoiceResponse>> UpdateInvoiceItem(
        long id, long itemId, UpdateInvoiceItemRequest request, CancellationToken ct);

    Task<Result<InvoiceResponse>> DeleteInvoiceItem(long id, long itemId, CancellationToken ct);
}
