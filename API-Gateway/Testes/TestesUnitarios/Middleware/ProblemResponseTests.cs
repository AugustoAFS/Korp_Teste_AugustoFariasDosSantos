using System.Text.Json;
using Gateway.Exceptions;
using Gateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gateway.TestesUnitarios.Middleware;

public sealed class ProblemResponseTests
{
    private static DefaultHttpContext Contexto()
    {
        var servicos = new ServiceCollection();
        servicos.AddProblemDetails();
        servicos.AddLogging();

        return new DefaultHttpContext
        {
            RequestServices = servicos.BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
            Request = { Path = "/api/v1/auth/login" }
        };
    }

    private static async Task<JsonElement> Corpo(HttpContext contexto)
    {
        contexto.Response.Body.Seek(0, SeekOrigin.Begin);
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        return documento.RootElement.Clone();
    }

    [Fact]
    public async Task Escreve_o_status_do_erro_na_resposta()
    {
        var contexto = Contexto();

        await ProblemResponse.Write(contexto, Errors.InvalidSession);

        contexto.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Corpo_traz_o_code_legivel_por_maquina()
    {
        var contexto = Contexto();

        await ProblemResponse.Write(contexto, Errors.InvalidSession);

        (await Corpo(contexto)).GetProperty("code").GetString().ShouldBe("invalid_session");
    }

    [Fact]
    public async Task Corpo_traz_traceId_e_o_caminho_requisitado()
    {
        var contexto = Contexto();

        await ProblemResponse.Write(contexto, Errors.Forbidden);

        var corpo = await Corpo(contexto);
        corpo.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        corpo.GetProperty("instance").GetString().ShouldBe("/api/v1/auth/login");
    }

    [Fact]
    public async Task Corpo_traz_titulo_e_detalhe_do_catalogo()
    {
        var contexto = Contexto();

        await ProblemResponse.Write(contexto, Errors.UserLocked);

        var corpo = await Corpo(contexto);
        corpo.GetProperty("title").GetString().ShouldBe(Errors.UserLocked.Title);
        corpo.GetProperty("detail").GetString().ShouldBe(Errors.UserLocked.Detail);
    }

    [Fact]
    public async Task Resposta_ja_iniciada_nao_e_sobrescrita()
    {
        var contexto = Contexto();
        contexto.Features.Set<IHttpResponseFeature>(new RespostaJaIniciada());

        await ProblemResponse.Write(contexto, Errors.InternalError);

        contexto.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Escrita_sem_limpar_preserva_o_que_ja_estava_no_corpo()
    {
        var contexto = Contexto();

        await ProblemResponse.Write(contexto, Errors.TooManyRequests, clearResponse: false);

        contexto.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    private sealed class RespostaJaIniciada : IHttpResponseFeature
    {
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public string? ReasonPhrase { get; set; }
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public void OnStarting(Func<object, Task> callback, object state) { }
    }
}
