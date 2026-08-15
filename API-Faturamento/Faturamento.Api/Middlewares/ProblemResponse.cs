using System.Diagnostics;
using Faturamento.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.Api.Middlewares;

public static class ProblemResponse
{
    public static async ValueTask Write(HttpContext context, Error erro, bool limparResposta = true)
    {
        if (context.Response.HasStarted) return;

        if (limparResposta) context.Response.Clear();

        context.Response.StatusCode = (int)erro.Status;

        await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = (int)erro.Status,
                Title = erro.Title,
                Detail = erro.Detail,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["code"] = erro.Code,
                    ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
                }
            }
        });
    }
}
