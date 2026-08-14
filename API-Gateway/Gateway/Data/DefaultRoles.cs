using Gateway.Models.Enums;

namespace Gateway.Data;

public static class DefaultRoles
{
    public static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly (long Id, string Name, string Description)[] All =
    [
        ((long)DefaultRole.Administrador,
            nameof(DefaultRole.Administrador),
            "Acesso total, inclusive à administração de usuários."),

        ((long)DefaultRole.Gerente,
            nameof(DefaultRole.Gerente),
            "Mantém o cadastro de produtos e acompanha as notas fiscais da equipe."),

        ((long)DefaultRole.Funcionario,
            nameof(DefaultRole.Funcionario),
            "Emite notas fiscais.")
    ];
}
