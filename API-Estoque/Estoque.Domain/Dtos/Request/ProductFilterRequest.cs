using System.ComponentModel.DataAnnotations;

namespace Estoque.Domain.Dtos.Request;

public sealed record ProductFilterRequest
{
    public const int MaxSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior que zero.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxSize, ErrorMessage = "O tamanho deve estar entre 1 e 100.")]
    public int Size { get; init; } = 20;

    [MaxLength(200, ErrorMessage = "O termo de busca excede o tamanho permitido.")]
    public string? Search { get; init; }
}
