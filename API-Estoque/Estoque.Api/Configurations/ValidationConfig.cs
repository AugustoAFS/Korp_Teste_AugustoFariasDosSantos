using System.Diagnostics;
using Estoque.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Api.Configurations;

public static class ValidationConfig
{
    public static IServiceCollection AddValidationContract(this IServiceCollection services)
        => services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
        {
            var erro = Errors.ValidationFailed;

            var problem = new ProblemDetails
            {
                Status = (int)erro.Status,
                Title = erro.Title,
                Detail = erro.Detail,
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["code"] = erro.Code;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            problem.Extensions["errors"] = context.ModelState
                .Where(entrada => entrada.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entrada => entrada.Key,
                    entrada => entrada.Value!.Errors.Select(falha => falha.ErrorMessage).ToArray());

            return new ObjectResult(problem) { StatusCode = (int)erro.Status };
        });
}
