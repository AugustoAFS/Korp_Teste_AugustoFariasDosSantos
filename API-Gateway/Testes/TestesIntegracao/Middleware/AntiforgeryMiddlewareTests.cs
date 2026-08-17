using System.Net;
using System.Net.Http.Json;
using Gateway.Dtos.Request;
using Gateway.Middleware;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gateway.TestesIntegracao.Middleware;

[Collection(AmbienteCollection.Nome)]
public sealed class AntiforgeryMiddlewareTests : IAsyncLifetime, IDisposable
{
    private const string Senha = "Senha@123";
    private const string Email = "antiforgery@korp.com.br";

    private readonly PostgresFixture _banco;
    private readonly GatewayApiFactory _api;

    public AntiforgeryMiddlewareTests(PostgresFixture banco)
    {
        _banco = banco;
        _api = new GatewayApiFactory(banco);
    }

    public async Task InitializeAsync() => await _banco.LimparUsuarios();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _api.Dispose();

    private async Task<HttpClient> Autenticado()
    {
        var cliente = _api.Cliente();

        await cliente.PostAsJsonAsync("/api/v1/users", new CreateUserRequest
        {
            Name = "Augusto", Email = Email, Password = Senha, Roles = []
        });

        await cliente.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest { Email = Email, Password = Senha });

        return cliente;
    }

    private static async Task<string> Token(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        var cookie = resposta.Headers.GetValues("Set-Cookie")
            .First(item => item.StartsWith(AntiforgeryMiddleware.TokenCookie, StringComparison.Ordinal));

        return cookie.Split(';')[0].Split('=', 2)[1];
    }

    #region Métodos seguros

    [Fact]
    public async Task Metodo_seguro_passa_sem_token()
    {
        var cliente = await Autenticado();

        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metodo_seguro_publica_o_cookie_para_a_proxima_mutacao()
    {
        var cliente = await Autenticado();

        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        resposta.Headers.GetValues("Set-Cookie")
            .ShouldContain(cookie =>
                cookie.StartsWith(AntiforgeryMiddleware.TokenCookie, StringComparison.Ordinal));
    }

    #endregion

    #region Mutação autenticada

    [Fact]
    public async Task Mutacao_sem_o_header_e_recusada_com_codigo_proprio()
    {
        var cliente = await Autenticado();

        var resposta = await cliente.PostAsync("/api/v1/auth/logout", null);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("invalid_antiforgery_token");
    }

    [Fact]
    public async Task Mutacao_com_header_invalido_tambem_e_recusada()
    {
        var cliente = await Autenticado();
        await Token(cliente);

        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        requisicao.Headers.Add(AntiforgeryMiddleware.TokenHeader, "token-inventado");

        (await cliente.SendAsync(requisicao)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Mutacao_com_o_header_correto_passa()
    {
        var cliente = await Autenticado();

        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        requisicao.Headers.Add(AntiforgeryMiddleware.TokenHeader, await Token(cliente));

        (await cliente.SendAsync(requisicao)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Mutação anônima

    [Fact]
    public async Task Cadastro_anonimo_nao_exige_antiforgery_porque_nao_ha_sessao()
    {
        var resposta = await _api.Cliente().PostAsJsonAsync("/api/v1/users", new CreateUserRequest
        {
            Name = "Anônimo", Email = "anonimo@korp.com.br", Password = Senha, Roles = []
        });

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_nao_exige_antiforgery()
    {
        var cliente = _api.Cliente();

        await cliente.PostAsJsonAsync("/api/v1/users", new CreateUserRequest
        {
            Name = "Augusto", Email = Email, Password = Senha, Roles = []
        });

        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest { Email = Email, Password = Senha });

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion
}
