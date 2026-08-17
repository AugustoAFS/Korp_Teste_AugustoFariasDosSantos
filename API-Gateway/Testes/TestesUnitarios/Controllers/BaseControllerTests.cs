using Gateway.Controllers;
using Gateway.Dtos;
using Gateway.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shouldly;

namespace Gateway.TestesUnitarios.Controllers;

public sealed class BaseControllerTests
{
    private readonly ControladorDeTeste _controller = new();

    public BaseControllerTests()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/users";

        _controller.ControllerContext = new ControllerContext { HttpContext = contexto };
        _controller.ProblemDetailsFactory = new FabricaDeProblema();
    }

    private static ProblemDetails Problema(IActionResult resposta)
        => resposta.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();

    #region Sucesso

    [Fact]
    public void Result_de_sucesso_vira_apenas_o_status()
        => _controller.Executar(Result.NoContent())
            .ShouldBeOfType<StatusCodeResult>()
            .StatusCode.ShouldBe(StatusCodes.Status204NoContent);

    [Fact]
    public void Result_generico_de_sucesso_leva_o_valor_no_corpo()
    {
        var resposta = _controller.Executar(Result<string>.Ok("augusto@korp.com.br"));

        var objeto = resposta.ShouldBeOfType<ObjectResult>();
        objeto.StatusCode.ShouldBe(StatusCodes.Status200OK);
        objeto.Value.ShouldBe("augusto@korp.com.br");
    }

    [Fact]
    public void Created_preserva_o_201()
        => _controller.Executar(Result<string>.Created("x"))
            .ShouldBeOfType<ObjectResult>()
            .StatusCode.ShouldBe(StatusCodes.Status201Created);

    #endregion

    #region Erro

    [Theory]
    [InlineData("invalid_credentials", StatusCodes.Status401Unauthorized)]
    [InlineData("forbidden", StatusCodes.Status403Forbidden)]
    [InlineData("user_not_found", StatusCodes.Status404NotFound)]
    [InlineData("email_in_use", StatusCodes.Status409Conflict)]
    [InlineData("role_not_found", StatusCodes.Status422UnprocessableEntity)]
    [InlineData("service_unavailable", StatusCodes.Status503ServiceUnavailable)]
    public void Cada_erro_do_catalogo_leva_o_proprio_status(string codigo, int status)
    {
        var erro = Catalogo().First(item => item.Code == codigo);

        var resposta = _controller.Executar((Result)erro);

        resposta.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(status);
        Problema(resposta).Extensions["code"].ShouldBe(codigo);
    }

    [Fact]
    public void Erro_expoe_o_traceId_para_correlacionar_com_o_log()
        => Problema(_controller.Executar((Result)Errors.InternalError)).Extensions["traceId"].ShouldNotBeNull();

    [Fact]
    public void Erro_registra_o_caminho_requisitado_em_instance()
        => Problema(_controller.Executar((Result)Errors.UserNotFound)).Instance.ShouldBe("/api/v1/users");

    [Fact]
    public void Erro_leva_titulo_e_detalhe_do_catalogo_para_o_usuario()
    {
        var problema = Problema(_controller.Executar((Result)Errors.EmailInUse));

        problema.Title.ShouldBe(Errors.EmailInUse.Title);
        problema.Detail.ShouldBe(Errors.EmailInUse.Detail);
    }

    [Fact]
    public void Detalhe_enriquecido_com_With_chega_na_resposta()
        => Problema(_controller.Executar((Result)Errors.RoleNotFound.With("O perfil Supervisor não existe.")))
            .Detail.ShouldBe("O perfil Supervisor não existe.");

    [Fact]
    public void Result_generico_com_erro_nao_devolve_valor()
        => Problema(_controller.Executar((Result<string>)Errors.UserNotFound)).ShouldNotBeNull();

    #endregion

    private static IEnumerable<Error> Catalogo()
        => typeof(Errors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(campo => campo.FieldType == typeof(Error))
            .Select(campo => (Error)campo.GetValue(null)!);

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
