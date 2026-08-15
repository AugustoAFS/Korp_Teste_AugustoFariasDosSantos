using System.ComponentModel.DataAnnotations;

namespace Faturamento.Domain.Dtos.Request;

public sealed record UpdateInvoiceItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public int Quantity { get; init; }
}
