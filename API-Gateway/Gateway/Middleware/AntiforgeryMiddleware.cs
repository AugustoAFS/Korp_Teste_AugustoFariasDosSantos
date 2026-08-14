using Gateway.Config;
using Gateway.Exceptions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Middleware;

public sealed class AntiforgeryMiddleware(
    RequestDelegate next,
    IAntiforgery antiforgery,
    IWebHostEnvironment environment,
    ILogger<AntiforgeryMiddleware> logger)
{
    public const string TokenCookie = "XSRF-TOKEN";
    public const string TokenHeader = "X-XSRF-TOKEN";

    private const string HealthSegment = "/health";

    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace };

    public async Task InvokeAsync(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method))
        {
            if (!context.Request.Path.StartsWithSegments(HealthSegment))
                PublishToken(context);

            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true && !Exempt(context))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException ex)
            {
                logger.LogWarning(ex, "Antiforgery recusado em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);
                await ProblemResponse.Write(context, Errors.InvalidAntiforgeryToken);
                return;
            }
        }

        await next(context);
    }

    private static bool Exempt(HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<IgnoreAntiforgeryTokenAttribute>() is not null;

    private void PublishToken(HttpContext context)
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken;
        if (token is null) return;

        context.Response.Cookies.Append(TokenCookie, token, new CookieOptions
        {
            HttpOnly = false,
            Secure = AuthConfig.SecurePolicy(environment) == CookieSecurePolicy.Always || context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }
}
