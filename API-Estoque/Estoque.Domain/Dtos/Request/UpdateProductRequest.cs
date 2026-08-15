using System.ComponentModel.DataAnnotations;

namespace Estoque.Domain.Dtos.Request;

public sealed record UpdateProductRequest
{
    [Required(ErrorMessage = "Informe o código.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "O código deve ter entre 1 e 50 caracteres.")]
    public string Code { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "A descrição deve ter entre 3 e 200 caracteres.")]
    public string Description { get; init; } = string.Empty;

    public bool Active { get; init; } = true;
}
