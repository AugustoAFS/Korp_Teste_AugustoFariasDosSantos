using Gateway.Models.Enums;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Gateway.TestesIntegracao.Data;

[Collection(AmbienteCollection.Nome)]
public sealed class AdminSeederTests : IAsyncLifetime, IDisposable
{
    private const string EmailDoAdmin = "admin@admin.com";

    private readonly PostgresFixture _banco;
    private readonly GatewayApiFactory _api;

    public AdminSeederTests(PostgresFixture banco)
    {
        _banco = banco;
        _api = new GatewayApiFactory(banco);
    }

    public async Task InitializeAsync()
    {
        await _banco.LimparUsuarios();

        using var _ = _api.Cliente();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Boot_cria_a_conta_administrativa_de_teste()
    {
        await using var contexto = _banco.CreateContext();

        var admin = await contexto.Users.FirstOrDefaultAsync(usuario => usuario.Email == EmailDoAdmin);

        admin.ShouldNotBeNull();
        admin.Active.ShouldBeTrue();
    }

    [Fact]
    public async Task Administrador_semeado_recebe_o_perfil_de_administrador()
    {
        await using var contexto = _banco.CreateContext();

        var admin = await contexto.Users
            .Include(usuario => usuario.Roles)
            .ThenInclude(vinculo => vinculo.Role)
            .FirstAsync(usuario => usuario.Email == EmailDoAdmin);

        admin.Roles.Select(vinculo => vinculo.Role.Name).ShouldContain(nameof(DefaultRole.Administrador));
    }

    [Fact]
    public async Task Senha_do_administrador_e_gravada_com_hash_e_nunca_em_texto_puro()
    {
        await using var contexto = _banco.CreateContext();

        var admin = await contexto.Users.FirstAsync(usuario => usuario.Email == EmailDoAdmin);

        admin.PasswordHash.ShouldNotBeNull();
        admin.PasswordHash.ShouldStartWith("$argon2id$");
        admin.PasswordHash.ShouldNotContain("Admin123!");
    }

    [Fact]
    public async Task Segundo_boot_nao_duplica_o_administrador()
    {
        using var outroApp = new GatewayApiFactory(_banco);
        using var _ = outroApp.Cliente();

        await using var contexto = _banco.CreateContext();

        (await contexto.Users.CountAsync(usuario => usuario.Email == EmailDoAdmin)).ShouldBe(1);
    }

    [Fact]
    public async Task Banco_sobe_somente_com_a_conta_de_teste_sem_dados_de_exemplo()
    {
        await using var contexto = _banco.CreateContext();

        (await contexto.Users.CountAsync()).ShouldBe(1);
    }
}
