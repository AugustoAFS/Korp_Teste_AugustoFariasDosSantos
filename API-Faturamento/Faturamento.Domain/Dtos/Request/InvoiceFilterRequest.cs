using System.ComponentModel.DataAnnotations;
using Faturamento.Domain.Enums;

namespace Faturamento.Domain.Dtos.Request;

public sealed record InvoiceFilterRequest
{
    public const int MaxSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior que zero.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxSize, ErrorMessage = "O tamanho deve estar entre 1 e 100.")]
    public int Size { get; init; } = 20;

    public InvoiceStatus? Status { get; init; }
}
