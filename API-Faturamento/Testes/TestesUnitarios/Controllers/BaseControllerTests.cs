using System.Net;
using Faturamento.Api.Controllers;
using Faturamento.Domain.Dtos;
using Faturamento.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shouldly;

namespace Faturamento.TestesUnitarios.Controllers;

public sealed class BaseControllerTests
{
    private readonly ControladorDeTeste _controller = new();

    public BaseControllerTests()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/notas";

        _controller.ControllerContext = new ControllerContext { HttpContext = contexto };
        _controller.ProblemDetailsFactory = new FabricaDeProblema();
    }

    #region Sucesso

    [Fact]
    public void Result_de_sucesso_vira_apenas_o_status()
    {
        var resposta = _controller.Executar(Result.NoContent());

        var status = resposta.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe((int)HttpStatusCode.NoContent);
    }

    [Fact]
    public void Result_generico_de_sucesso_leva_o_valor_no_corpo()
    {
        var resposta = _controller.Executar(Result<string>.Ok("NF-1"));

        var objeto = resposta.ShouldBeOfType<ObjectResult>();
        objeto.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        objeto.Value.ShouldBe("NF-1");
    }

    [Fact]
    public void Created_preserva_o_201()
    {
        var resposta = _controller.Executar(Result<string>.Created("NF-1"));

        resposta.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe((int)HttpStatusCode.Created);
    }

    #endregion

    #region Erro

    [Fact]
    public void Erro_vira_problem_details_com_o_status_do_catalogo()
    {
        var resposta = _controller.Executar((Result)Errors.InvoiceNotFound);

        var objeto = resposta.ShouldBeOfType<ObjectResult>();
        objeto.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
        objeto.Value.ShouldBeOfType<ProblemDetails>().Status.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public void Erro_expoe_o_code_legivel_por_maquina()
    {
        var resposta = _controller.Executar((Result)Errors.InvoiceNotFound);

        var problema = resposta.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problema.Extensions["code"].ShouldBe("invoice_not_found");
    }

    [Fact]
    public void Erro_expoe_o_traceId_para_correlacionar_com_o_log()
    {
        var resposta = _controller.Executar((Result)Errors.InternalError);

        var problema = resposta.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problema.Extensions["traceId"].ShouldNotBeNull();
    }

    [Fact]
    public void Erro_registra_o_caminho_requisitado_em_instance()
    {
        var resposta = _controller.Executar((Result)Errors.InvoiceNotFound);

        var problema = resposta.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problema.Instance.ShouldBe("/api/v1/notas");
    }

    [Fact]
    public void Erro_leva_titulo_e_detalhe_do_catalogo_para_o_usuario()
    {
        var resposta = _controller.Executar((Result)Errors.InvoiceEmpty);

        var problema = resposta.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problema.Title.ShouldBe(Errors.InvoiceEmpty.Title);
        problema.Detail.ShouldBe(Errors.InvoiceEmpty.Detail);
    }

    [Fact]
    public void Detalhe_enriquecido_com_With_chega_na_resposta()
    {
        var erro = Errors.InvoiceAlreadyClosed.With("Esta nota já foi impressa em 10/08.");

        var resposta = _controller.Executar((Result)erro);

        var problema = resposta.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problema.Detail.ShouldBe("Esta nota já foi impressa em 10/08.");
    }

    [Fact]
    public void Result_generico_com_erro_nao_devolve_valor()
    {
        var resposta = _controller.Executar((Result<string>)Errors.InvoiceNotFound);

        resposta.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
    }

    #endregion

    private sealed class ControladorDeTeste : BaseController
    {
        public IActionResult Executar(Result resultado) => Respond(resultado);

        public IActionResult Executar<T>(Result<T> resultado) => Respond(resultado);
    }

    private sealed class FabricaDeProblema : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new()
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
    }
}
