using System.Diagnostics;
using Gateway.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Middleware;

public static class ProblemResponse
{
    public static async ValueTask Write(HttpContext context, Error error, bool clearResponse = true)
    {
        if (context.Response.HasStarted) return;

        if (clearResponse) context.Response.Clear();

        context.Response.StatusCode = error.Status;

        await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = error.Status,
                Title = error.Title,
                Detail = error.Detail,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["code"] = error.Code,
                    ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
                }
            }
        });
    }
}
