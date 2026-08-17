using System.Net;
using System.Net.Http.Json;
using Gateway.Dtos.Request;
using Gateway.Middleware;
using Gateway.Models.Enums;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gateway.TestesIntegracao.Controllers;

public sealed record SessaoNaResposta(string Name, string Email, IReadOnlyList<string> Roles);

public sealed record TokenNaResposta(string Token, int ExpiresIn);

[Collection(AmbienteCollection.Nome)]
public sealed class AuthControllerTests : IAsyncLifetime, IDisposable
{
    private const string Senha = "Senha@123";

    private readonly PostgresFixture _banco;
    private readonly GatewayApiFactory _api;

    public AuthControllerTests(PostgresFixture banco)
    {
        _banco = banco;
        _api = new GatewayApiFactory(banco);
    }

    public async Task InitializeAsync() => await _banco.LimparUsuarios();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _api.Dispose();

    private async Task<HttpClient> Cadastrado(string email = "augusto@korp.com.br")
    {
        var cliente = _api.Cliente();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/users", new CreateUserRequest
        {
            Name = "Augusto",
            Email = email,
            Password = Senha,
            Roles = []
        });

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        return cliente;
    }

    private static Task<HttpResponseMessage> Entrar(HttpClient cliente, string email, string senha)
        => cliente.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = email, Password = senha });

    private static async Task<string> TokenAntiforgery(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/v1/auth/me");
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var cookie = resposta.Headers.GetValues("Set-Cookie")
            .First(item => item.StartsWith(AntiforgeryMiddleware.TokenCookie, StringComparison.Ordinal));

        return cookie.Split(';')[0].Split('=', 2)[1];
    }

    #region Login

    [Fact]
    public async Task Login_com_credencial_valida_devolve_204_e_o_cookie_de_sessao()
    {
        var cliente = await Cadastrado();

        var resposta = await Entrar(cliente, "augusto@korp.com.br", Senha);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        resposta.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        cookies!.ShouldContain(cookie => cookie.Contains("SameSite=Strict", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_com_senha_errada_devolve_401_com_invalid_credentials()
    {
        var cliente = await Cadastrado();

        var resposta = await Entrar(cliente, "augusto@korp.com.br", "Errada@123");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("invalid_credentials");
    }

    [Fact]
    public async Task Login_de_email_inexistente_devolve_a_mesma_resposta_de_senha_errada()
    {
        var resposta = await Entrar(_api.Cliente(), "ninguem@korp.com.br", Senha);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("invalid_credentials");
    }

    [Fact]
    public async Task Sequencia_de_tentativas_e_barrada_pelo_rate_limiter_antes_do_bloqueio()
    {
        var cliente = await Cadastrado();

        HttpResponseMessage? barrada = null;

        for (var tentativa = 0; tentativa < 15 && barrada is null; tentativa++)
        {
            var resposta = await Entrar(cliente, "augusto@korp.com.br", "Errada@123");

            if (resposta.StatusCode == HttpStatusCode.TooManyRequests) barrada = resposta;
        }

        barrada.ShouldNotBeNull();

        var problema = await barrada.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("too_many_requests");
    }

    #endregion

    #region Sessão

    [Fact]
    public async Task Rota_protegida_sem_sessao_devolve_401()
    {
        var resposta = await _api.Cliente().GetAsync("/api/v1/auth/me");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("invalid_session");
    }

    [Fact]
    public async Task Sessao_aberta_devolve_nome_email_e_o_perfil_padrao()
    {
        var cliente = await Cadastrado();
        await Entrar(cliente, "augusto@korp.com.br", Senha);

        var sessao = await cliente.GetFromJsonAsync<SessaoNaResposta>("/api/v1/auth/me");

        sessao!.Name.ShouldBe("Augusto");
        sessao.Email.ShouldBe("augusto@korp.com.br");
        sessao.Roles.ShouldBe([nameof(DefaultRole.Funcionario)]);
    }

    [Fact]
    public async Task Cadastro_aberto_nunca_concede_administrador()
    {
        var cliente = _api.Cliente();
        await cliente.PostAsJsonAsync("/api/v1/users", new CreateUserRequest
        {
            Name = "Esperto",
            Email = "esperto@korp.com.br",
            Password = Senha,
            Roles = [nameof(DefaultRole.Administrador)]
        });

        await Entrar(cliente, "esperto@korp.com.br", Senha);

        var sessao = await cliente.GetFromJsonAsync<SessaoNaResposta>("/api/v1/auth/me");

        sessao!.Roles.ShouldBe([nameof(DefaultRole.Funcionario)]);
    }

    #endregion

    #region Token interno

    [Fact]
    public async Task Token_interno_exige_sessao()
        => (await _api.Cliente().GetAsync("/api/v1/auth/token")).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Token_interno_e_emitido_para_quem_tem_sessao()
    {
        var cliente = await Cadastrado();
        await Entrar(cliente, "augusto@korp.com.br", Senha);

        var token = await cliente.GetFromJsonAsync<TokenNaResposta>("/api/v1/auth/token");

        token!.Token.ShouldNotBeNullOrWhiteSpace();
        token.Token.Split('.').Length.ShouldBe(3);
        token.ExpiresIn.ShouldBe(120);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_com_token_de_antiforgery_encerra_a_sessao()
    {
        var cliente = await Cadastrado();
        await Entrar(cliente, "augusto@korp.com.br", Senha);

        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        requisicao.Headers.Add(AntiforgeryMiddleware.TokenHeader, await TokenAntiforgery(cliente));

        var logout = await cliente.SendAsync(requisicao);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await cliente.GetAsync("/api/v1/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Antiforgery

    [Fact]
    public async Task Requisicao_segura_autenticada_publica_o_cookie_de_antiforgery()
    {
        var cliente = await Cadastrado();
        await Entrar(cliente, "augusto@korp.com.br", Senha);

        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        resposta.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        cookies!.ShouldContain(cookie => cookie.StartsWith(AntiforgeryMiddleware.TokenCookie, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Mutacao_autenticada_sem_o_header_e_recusada()
    {
        var cliente = await Cadastrado();
        await Entrar(cliente, "augusto@korp.com.br", Senha);

        var resposta = await cliente.PostAsync("/api/v1/auth/logout", null);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("invalid_antiforgery_token");
    }

    [Fact]
    public async Task Health_nao_publica_token_de_antiforgery()
    {
        var resposta = await _api.Cliente().GetAsync("/health/live");

        if (resposta.Headers.TryGetValues("Set-Cookie", out var cookies))
            cookies.ShouldNotContain(
                cookie => cookie.StartsWith(AntiforgeryMiddleware.TokenCookie, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_e_isento_de_antiforgery_porque_ainda_nao_ha_sessao()
    {
        var cliente = await Cadastrado();

        var resposta = await Entrar(cliente, "augusto@korp.com.br", Senha);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion
}
