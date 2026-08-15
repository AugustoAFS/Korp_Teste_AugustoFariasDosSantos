using System.ComponentModel.DataAnnotations;

namespace Faturamento.Domain.Dtos.Request;

public sealed record AddInvoiceItemRequest
{
    [Required(ErrorMessage = "Informe o produto.")]
    public Guid ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public int Quantity { get; init; }
}
