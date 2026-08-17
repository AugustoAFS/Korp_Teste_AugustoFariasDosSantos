using System.Net;
using System.Net.Http.Json;
using Gateway.Dtos.Request;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gateway.TestesIntegracao.Config;

[Collection(AmbienteCollection.Nome)]
public sealed class ResilienceConfigTests(PostgresFixture banco) : IAsyncLifetime
{
    private const string Senha = "Senha@123";
    private const string RotaProxiada = "/api/v1/produtos";

    private DownstreamStub _estoque = null!;
    private GatewayApiFactory _api = null!;
    private HttpClient _cliente = null!;

    public async Task InitializeAsync()
    {
        await banco.LimparUsuarios();

        _estoque = new DownstreamStub();
        await _estoque.Iniciar();

        _api = new GatewayApiFactory(banco, _estoque.Endereco);
        _cliente = _api.Cliente();

        await _cliente.PostAsJsonAsync("/api/v1/users", new CreateUserRequest
        {
            Name = "Augusto",
            Email = "augusto@korp.com.br",
            Password = Senha,
            Roles = []
        });

        await _cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = "augusto@korp.com.br", Password = Senha });
    }

    public async Task DisposeAsync()
    {
        _api.Dispose();
        await _estoque.DisposeAsync();
    }

    private async Task<HttpStatusCode> Chamar()
        => (await _cliente.GetAsync(RotaProxiada)).StatusCode;

    private async Task AbrirOCircuito()
    {
        _estoque.Falhando = true;

        for (var tentativa = 0; tentativa < 10; tentativa++)
            await Chamar();
    }

    #region Proxy saudável

    [Fact]
    public async Task Rota_proxiada_alcanca_o_downstream_quando_ele_esta_no_ar()
    {
        var status = await Chamar();

        status.ShouldBe(HttpStatusCode.OK);
        _estoque.Chamadas.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Rota_proxiada_exige_sessao()
    {
        var anonimo = _api.Cliente();

        (await anonimo.GetAsync(RotaProxiada)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Circuito aberto

    [Fact]
    public async Task Downstream_falhando_faz_o_gateway_devolver_503_com_service_unavailable()
    {
        _estoque.Falhando = true;

        HttpResponseMessage? indisponivel = null;

        for (var tentativa = 0; tentativa < 10 && indisponivel is null; tentativa++)
        {
            var resposta = await _cliente.GetAsync(RotaProxiada);

            if (resposta.StatusCode == HttpStatusCode.ServiceUnavailable) indisponivel = resposta;
        }

        indisponivel.ShouldNotBeNull();

        var problema = await indisponivel.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("service_unavailable");
    }

    [Fact]
    public async Task Circuito_aberto_para_de_bater_no_downstream()
    {
        await AbrirOCircuito();

        _estoque.ZerarContador();

        await Chamar();
        await Chamar();

        _estoque.Chamadas.ShouldBe(0);
    }

    [Fact]
    public async Task Circuito_aberto_responde_imediatamente_em_vez_de_esperar_timeout()
    {
        await AbrirOCircuito();

        var relogio = System.Diagnostics.Stopwatch.StartNew();
        await Chamar();
        relogio.Stop();

        relogio.ElapsedMilliseconds.ShouldBeLessThan(1000);
    }

    [Fact]
    public async Task Resposta_de_circuito_aberto_tem_corpo_e_nao_um_502_vazio()
    {
        await AbrirOCircuito();

        var resposta = await _cliente.GetAsync(RotaProxiada);

        resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await resposta.Content.ReadAsStringAsync()).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Erro_do_proxy_traz_traceId_como_todo_erro_do_gateway()
    {
        await AbrirOCircuito();

        var resposta = await _cliente.GetAsync(RotaProxiada);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();

        problema!.Extensions["traceId"].ShouldNotBeNull();
    }

    #endregion
}
