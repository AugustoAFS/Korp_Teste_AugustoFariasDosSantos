using System.Net.Http.Headers;
using Gateway.Security.Interfaces;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

namespace Gateway.Config;

public static class ReverseProxyConfig
{
    public const string FrontCluster = "notaflow";

    private const string Section = "ReverseProxy";
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddDownstreamProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var proxy = configuration.GetSection(Section);

        if (!proxy.Exists())
            throw new InvalidOperationException("ReverseProxy não configurado.");

        services
            .AddReverseProxy()
            .LoadFromConfig(proxy)
            .AddTransforms(transforms => transforms.AddRequestTransform(SignInternalToken));

        return services;
    }

    private static ValueTask SignInternalToken(RequestTransformContext context)
    {
        if (ServesStaticFront(context)) return ValueTask.CompletedTask;

        context.ProxyRequest.Headers.Remove(HeaderNames.Cookie);

        var principal = context.HttpContext.User;

        if (principal.Identity?.IsAuthenticated != true) return ValueTask.CompletedTask;

        var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();

        context.ProxyRequest.Headers.Authorization =
            new AuthenticationHeaderValue(BearerScheme, tokenService.Issue(principal));

        return ValueTask.CompletedTask;
    }

    private static bool ServesStaticFront(RequestTransformContext context)
        => context.HttpContext.GetReverseProxyFeature().Route.Config.ClusterId == FrontCluster;
}
