using Gateway.Models.Enums;
using Gateway.Repositories;
using Gateway.TestesIntegracao.Suporte;
using Shouldly;

namespace Gateway.TestesIntegracao.Repositories;

[Collection(AmbienteCollection.Nome)]
public sealed class RoleRepositoryTests(PostgresFixture banco)
{
    [Fact]
    public async Task Os_tres_perfis_padrao_existem_apos_a_migracao()
    {
        await using var contexto = banco.CreateContext();

        var perfis = await new RoleRepository(contexto).ByNames(
            [nameof(DefaultRole.Administrador), nameof(DefaultRole.Gerente), nameof(DefaultRole.Funcionario)],
            default);

        perfis.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Perfil_e_encontrado_pelo_nome_exato()
    {
        await using var contexto = banco.CreateContext();

        var perfis = await new RoleRepository(contexto).ByNames([nameof(DefaultRole.Gerente)], default);

        perfis.Single().Name.ShouldBe("Gerente");
        perfis.Single().Id.ShouldBe((long)DefaultRole.Gerente);
    }

    [Fact]
    public async Task Nome_inexistente_nao_devolve_nada()
    {
        await using var contexto = banco.CreateContext();

        (await new RoleRepository(contexto).ByNames(["Supervisor"], default)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Nomes_repetidos_no_pedido_nao_duplicam_o_resultado()
    {
        await using var contexto = banco.CreateContext();

        var perfis = await new RoleRepository(contexto)
            .ByNames([nameof(DefaultRole.Gerente), nameof(DefaultRole.Gerente)], default);

        perfis.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Espacos_em_volta_do_nome_sao_ignorados()
    {
        await using var contexto = banco.CreateContext();

        var perfis = await new RoleRepository(contexto).ByNames(["  Gerente  "], default);

        perfis.Single().Name.ShouldBe("Gerente");
    }

    [Fact]
    public async Task Lista_vazia_devolve_nada_sem_explodir()
    {
        await using var contexto = banco.CreateContext();

        (await new RoleRepository(contexto).ByNames([], default)).ShouldBeEmpty();
    }
}
