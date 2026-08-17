using Gateway.Models;
using Gateway.Models.Enums;
using Shouldly;

namespace Gateway.TestesUnitarios.Models;

public sealed class RoleTests
{
    [Fact]
    public void Perfil_nasce_ativo()
        => new Role(1, "Administrador", "Acesso total").Active.ShouldBeTrue();

    [Fact]
    public void Perfil_guarda_id_nome_e_descricao()
    {
        var perfil = new Role(2, "Gerente", "Gerencia usuários e produtos");

        perfil.Id.ShouldBe(2);
        perfil.Name.ShouldBe("Gerente");
        perfil.Description.ShouldBe("Gerencia usuários e produtos");
    }

    [Fact]
    public void Perfil_aceita_descricao_nula()
        => new Role(3, "Funcionario", null).Description.ShouldBeNull();

    [Theory]
    [InlineData(DefaultRole.Administrador, 1)]
    [InlineData(DefaultRole.Gerente, 2)]
    [InlineData(DefaultRole.Funcionario, 3)]
    public void Perfis_padrao_tem_identificadores_estaveis(DefaultRole perfil, int esperado)
        => ((int)perfil).ShouldBe(esperado);

    [Fact]
    public void Nome_do_perfil_padrao_e_o_que_vai_para_a_claim_role()
    {
        nameof(DefaultRole.Administrador).ShouldBe("Administrador");
        nameof(DefaultRole.Gerente).ShouldBe("Gerente");
        nameof(DefaultRole.Funcionario).ShouldBe("Funcionario");
    }
}
