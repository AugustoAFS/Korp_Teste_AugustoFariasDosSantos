using System.Diagnostics;
using Estoque.Domain.Dtos;
using Estoque.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult Respond(Result resultado)
        => resultado.Success ? StatusCode((int)resultado.Status) : Problem(resultado.Error!);

    protected IActionResult Respond<T>(Result<T> resultado)
        => resultado.Success ? StatusCode((int)resultado.Status, resultado.Value) : Problem(resultado.Error!);

    private IActionResult Problem(Error erro)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode: (int)erro.Status,
            title: erro.Title,
            detail: erro.Detail);

        problem.Instance = HttpContext.Request.Path;
        problem.Extensions["code"] = erro.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        return new ObjectResult(problem) { StatusCode = (int)erro.Status };
    }
}
