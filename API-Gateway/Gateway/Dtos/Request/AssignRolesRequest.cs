using System.ComponentModel.DataAnnotations;

namespace Gateway.Dtos.Request;

public sealed record AssignRolesRequest
{
    [Required(ErrorMessage = "Informe os perfis.")]
    [MinLength(1, ErrorMessage = "Informe ao menos um perfil.")]
    public string[] Roles { get; init; } = [];
}
