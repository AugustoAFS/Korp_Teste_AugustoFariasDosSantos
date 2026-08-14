using System.Diagnostics;
using Gateway.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Config;

public static class ValidationConfig
{
    public static IServiceCollection AddValidationContract(this IServiceCollection services)
        => services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ProblemDetails
            {
                Status = Errors.ValidationFailed.Status,
                Title = Errors.ValidationFailed.Title,
                Detail = Errors.ValidationFailed.Detail,
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["code"] = Errors.ValidationFailed.Code;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            problem.Extensions["errors"] = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            return new ObjectResult(problem) { StatusCode = Errors.ValidationFailed.Status };
        });
}
