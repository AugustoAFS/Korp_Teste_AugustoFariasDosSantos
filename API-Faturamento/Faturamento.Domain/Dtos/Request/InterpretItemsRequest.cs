using System.ComponentModel.DataAnnotations;

namespace Faturamento.Domain.Dtos.Request;

public sealed record InterpretItemsRequest
{
    [Required(ErrorMessage = "Descreva os itens que deseja incluir.")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "O texto deve ter entre 3 e 500 caracteres.")]
    public string Phrase { get; init; } = string.Empty;
}
