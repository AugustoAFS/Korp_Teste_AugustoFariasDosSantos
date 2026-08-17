using Gateway.Models.Enums;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Gateway.TestesIntegracao.Data;

[Collection(AmbienteCollection.Nome)]
public sealed class RoleSeederTests(PostgresFixture banco)
{
    [Fact]
    public async Task Os_tres_perfis_padrao_existem_apos_o_boot()
    {
        await using var contexto = banco.CreateContext();

        var nomes = await contexto.Roles.Select(perfil => perfil.Name).ToListAsync();

        nomes.ShouldBe(
            [nameof(DefaultRole.Administrador), nameof(DefaultRole.Gerente), nameof(DefaultRole.Funcionario)],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Identificador_de_cada_perfil_bate_com_o_enum()
    {
        await using var contexto = banco.CreateContext();

        foreach (var padrao in Enum.GetValues<DefaultRole>())
        {
            var perfil = await contexto.Roles.FirstAsync(candidato => candidato.Name == padrao.ToString());

            perfil.Id.ShouldBe((long)padrao);
        }
    }

    [Fact]
    public async Task Perfis_padrao_nascem_ativos()
    {
        await using var contexto = banco.CreateContext();

        (await contexto.Roles.AllAsync(perfil => perfil.Active)).ShouldBeTrue();
    }

    [Fact]
    public async Task Segundo_boot_nao_duplica_os_perfis()
    {
        using var app = new GatewayApiFactory(banco);
        using var _ = app.Cliente();

        await using var contexto = banco.CreateContext();

        (await contexto.Roles.CountAsync()).ShouldBe(Enum.GetValues<DefaultRole>().Length);
    }
}
