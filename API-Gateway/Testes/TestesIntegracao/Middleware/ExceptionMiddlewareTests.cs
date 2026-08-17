using System.Net;
using System.Net.Http.Json;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gateway.TestesIntegracao.Middleware;

[Collection(AmbienteCollection.Nome)]
public sealed class ExceptionMiddlewareTests : IDisposable
{
    private readonly GatewayApiFactory _api;

    public ExceptionMiddlewareTests(PostgresFixture banco) => _api = new GatewayApiFactory(banco);

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Rota_desconhecida_cai_no_front_e_nao_devolve_404_do_gateway()
    {
        var resposta = await _api.Cliente().GetAsync("/api/v1/rota-que-nao-existe");

        resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("service_unavailable");
    }

    [Fact]
    public async Task Nenhuma_resposta_de_erro_vaza_stack_trace()
    {
        var resposta = await _api.Cliente().GetAsync("/api/v1/rota-que-nao-existe");

        var corpo = await resposta.Content.ReadAsStringAsync();

        corpo.ShouldNotContain("at Gateway.");
        corpo.ShouldNotContain("StackTrace");
    }

    [Fact]
    public async Task Payload_malformado_devolve_400_e_nao_500()
    {
        var conteudo = new StringContent("{ isso não é json", System.Text.Encoding.UTF8, "application/json");

        var resposta = await _api.Cliente().PostAsync("/api/v1/users", conteudo);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Erro_de_validacao_traz_o_contrato_de_problem_details()
    {
        var resposta = await _api.Cliente().PostAsJsonAsync(
            "/api/v1/users",
            new { name = "", email = "nao-e-email", password = "1", roles = Array.Empty<string>() });

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Title.ShouldNotBeNullOrWhiteSpace();
        problema.Status.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Health_responde_sem_passar_pelo_tratamento_de_erro()
    {
        var resposta = await _api.Cliente().GetAsync("/health/live");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Requisicao_cancelada_pelo_cliente_nao_vira_erro_500()
    {
        using var cancelamento = new CancellationTokenSource();
        var chamada = _api.Cliente().GetAsync("/api/v1/auth/me", cancelamento.Token);

        await cancelamento.CancelAsync();

        try
        {
            var resposta = await chamada;
            ((int)resposta.StatusCode).ShouldBeLessThan(500);
        }
        catch (TaskCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }
}
