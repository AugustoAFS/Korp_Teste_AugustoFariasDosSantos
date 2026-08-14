using System.Diagnostics;
using Gateway.Dtos;
using Gateway.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult Respond(Result result)
        => result.Success ? StatusCode(result.Status) : Problem(result.Error!);

    protected IActionResult Respond<T>(Result<T> result)
        => result.Success ? StatusCode(result.Status, result.Value) : Problem(result.Error!);

    private IActionResult Problem(Error error)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode: error.Status,
            title: error.Title,
            detail: error.Detail);

        problem.Instance = HttpContext.Request.Path;
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        return new ObjectResult(problem) { StatusCode = error.Status };
    }
}
