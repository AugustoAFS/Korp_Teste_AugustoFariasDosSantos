using Faturamento.Domain.Entities;

namespace Faturamento.Domain.Dtos.Response;

public sealed record InvoiceItemResponse
{
    public InvoiceItemResponse(InvoiceItem item)
    {
        Id = item.Id;
        ProductId = item.ProductId;
        ProductCode = item.ProductCode;
        ProductDescription = item.ProductDescription;
        Quantity = item.Quantity;
    }

    public long Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductDescription { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
