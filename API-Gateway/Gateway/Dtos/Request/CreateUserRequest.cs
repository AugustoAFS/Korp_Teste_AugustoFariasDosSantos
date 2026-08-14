using System.ComponentModel.DataAnnotations;

namespace Gateway.Dtos.Request;

public sealed record CreateUserRequest
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 150 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(320, ErrorMessage = "E-mail excede o tamanho permitido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(200, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 200 caracteres.")]
    public string Password { get; init; } = string.Empty;

    public string[] Roles { get; init; } = [];
}
