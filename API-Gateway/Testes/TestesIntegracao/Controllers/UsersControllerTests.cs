using System.Net;
using System.Net.Http.Json;
using Gateway.Dtos.Request;
using Gateway.Middleware;
using Gateway.Models.Enums;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gateway.TestesIntegracao.Controllers;

public sealed record UsuarioNaResposta(long Id, string Name, string Email, bool Active, IReadOnlyList<string> Roles);

public sealed record PaginaDeUsuarios(
    IReadOnlyList<UsuarioNaResposta> Items, int Page, int Size, int Total, int TotalPages);

[Collection(AmbienteCollection.Nome)]
public sealed class UsersControllerTests : IAsyncLifetime, IDisposable
{
    private const string Rota = "/api/v1/users";
    private const string Senha = "Senha@123";

    private readonly PostgresFixture _banco;
    private readonly GatewayApiFactory _api;

    public UsersControllerTests(PostgresFixture banco)
    {
        _banco = banco;
        _api = new GatewayApiFactory(banco);
    }

    public async Task InitializeAsync() => await _banco.LimparUsuarios();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _api.Dispose();

    private async Task<HttpClient> Autenticado(string email, params string[] perfis)
    {
        var cliente = _api.Cliente();

        var criacao = await cliente.PostAsJsonAsync(Rota, new CreateUserRequest
        {
            Name = "Usuário de Teste",
            Email = email,
            Password = Senha,
            Roles = []
        });
        criacao.StatusCode.ShouldBe(HttpStatusCode.Created);

        if (perfis.Length > 0) await ConcederPerfis(email, perfis);

        var login = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest { Email = email, Password = Senha });
        login.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return cliente;
    }

    private async Task ConcederPerfis(string email, params string[] perfis)
    {
        await using var contexto = _banco.CreateContext();

        var usuario = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstAsync(contexto.Users, candidato => candidato.Email == email);

        usuario.ReplaceRoles([.. perfis.Select(perfil => (long)Enum.Parse<DefaultRole>(perfil))]);
        await contexto.SaveChangesAsync();
    }

    #region Cadastro aberto

    [Fact]
    public async Task Cadastro_e_anonimo_e_devolve_201()
    {
        var resposta = await _api.Cliente().PostAsJsonAsync(Rota, new CreateUserRequest
        {
            Name = "Augusto",
            Email = "augusto@korp.com.br",
            Password = Senha,
            Roles = []
        });

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioNaResposta>();
        usuario!.Roles.ShouldBe([nameof(DefaultRole.Funcionario)]);
    }

    [Fact]
    public async Task Email_repetido_devolve_409()
    {
        await _api.Cliente().PostAsJsonAsync(Rota, new CreateUserRequest
        {
            Name = "Augusto", Email = "augusto@korp.com.br", Password = Senha, Roles = []
        });

        var resposta = await _api.Cliente().PostAsJsonAsync(Rota, new CreateUserRequest
        {
            Name = "Outro", Email = "augusto@korp.com.br", Password = Senha, Roles = []
        });

        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("email_in_use");
    }

    [Fact]
    public async Task Senha_curta_e_recusada_na_validacao()
    {
        var resposta = await _api.Cliente().PostAsJsonAsync(Rota, new CreateUserRequest
        {
            Name = "Augusto", Email = "augusto@korp.com.br", Password = "123", Roles = []
        });

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Listagem por perfil

    [Fact]
    public async Task Listagem_exige_sessao()
        => (await _api.Cliente().GetAsync(Rota)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Funcionario_nao_pode_listar_usuarios()
    {
        var cliente = await Autenticado("funcionario@korp.com.br");

        (await cliente.GetAsync(Rota)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Gerente_pode_listar_usuarios()
    {
        var cliente = await Autenticado("gerente@korp.com.br", nameof(DefaultRole.Gerente));

        var pagina = await cliente.GetFromJsonAsync<PaginaDeUsuarios>(Rota);

        pagina!.Total.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Administrador_pode_listar_usuarios()
    {
        var cliente = await Autenticado("adm@korp.com.br", nameof(DefaultRole.Administrador));

        (await cliente.GetAsync(Rota)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Listagem_aceita_busca_por_nome_ou_email()
    {
        var cliente = await Autenticado("gerente@korp.com.br", nameof(DefaultRole.Gerente));

        var pagina = await cliente.GetFromJsonAsync<PaginaDeUsuarios>($"{Rota}?search=GERENTE");

        pagina!.Items.ShouldContain(usuario => usuario.Email == "gerente@korp.com.br");
    }

    #endregion

    #region Visibilidade individual

    [Fact]
    public async Task Funcionario_ve_o_proprio_cadastro()
    {
        var cliente = await Autenticado("funcionario@korp.com.br");
        var sessao = await cliente.GetFromJsonAsync<SessaoNaResposta>("/api/v1/auth/me");

        await using var contexto = _banco.CreateContext();
        var proprio = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstAsync(contexto.Users, usuario => usuario.Email == sessao!.Email);

        (await cliente.GetAsync($"{Rota}/{proprio.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Funcionario_nao_ve_o_cadastro_de_outro()
    {
        var outro = await Autenticado("outro@korp.com.br");
        _ = outro;

        long idDoOutro;
        await using (var contexto = _banco.CreateContext())
        {
            var usuario = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstAsync(contexto.Users, candidato => candidato.Email == "outro@korp.com.br");
            idDoOutro = usuario.Id;
        }

        var cliente = await Autenticado("funcionario@korp.com.br");

        var resposta = await cliente.GetAsync($"{Rota}/{idDoOutro}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("forbidden");
    }

    #endregion

    #region Troca de perfis

    [Fact]
    public async Task Somente_administrador_troca_perfis()
    {
        var gerente = await Autenticado("gerente@korp.com.br", nameof(DefaultRole.Gerente));

        var requisicao = new HttpRequestMessage(HttpMethod.Put, $"{Rota}/1/roles")
        {
            Content = JsonContent.Create(new AssignRolesRequest { Roles = [nameof(DefaultRole.Gerente)] })
        };

        var resposta = await gerente.SendAsync(requisicao);

        resposta.StatusCode.ShouldBeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Administrador_troca_os_perfis_de_outro_usuario()
    {
        await Autenticado("alvo@korp.com.br");

        long idDoAlvo;
        await using (var contexto = _banco.CreateContext())
        {
            var usuario = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstAsync(contexto.Users, candidato => candidato.Email == "alvo@korp.com.br");
            idDoAlvo = usuario.Id;
        }

        var adm = await Autenticado("adm@korp.com.br", nameof(DefaultRole.Administrador));

        var requisicao = new HttpRequestMessage(HttpMethod.Put, $"{Rota}/{idDoAlvo}/roles")
        {
            Content = JsonContent.Create(new AssignRolesRequest { Roles = [nameof(DefaultRole.Gerente)] })
        };
        requisicao.Headers.Add(AntiforgeryMiddleware.TokenHeader, await TokenDe(adm));

        var resposta = await adm.SendAsync(requisicao);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    private static async Task<string> TokenDe(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        var cookie = resposta.Headers.GetValues("Set-Cookie")
            .First(item => item.StartsWith(AntiforgeryMiddleware.TokenCookie, StringComparison.Ordinal));

        return cookie.Split(';')[0].Split('=', 2)[1];
    }
}
