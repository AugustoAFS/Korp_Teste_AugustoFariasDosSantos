using System.Globalization;
using System.Threading.RateLimiting;
using Estoque.Api.Middlewares;
using Estoque.Domain.Exceptions;
using Estoque.InfraStructure.Security;

namespace Estoque.Api.Configurations;

public static class RateLimitConfig
{
    private const int Requisicoes = 120;
    private const int JanelaEmSegundos = 60;

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        => services.AddRateLimiter(limiter =>
        {
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(Janela);

            limiter.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                await ProblemResponse.Write(context.HttpContext, Errors.TooManyRequests, limparResposta: false);
            };
        });

    private static RateLimitPartition<string> Janela(HttpContext context)
        => RateLimitPartition.GetFixedWindowLimiter(Particao(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Requisicoes,
            Window = TimeSpan.FromSeconds(JanelaEmSegundos),
            QueueLimit = 0
        });

    private static string Particao(HttpContext context)
        => context.User.FindFirst(CurrentUser.Claim)?.Value
           ?? context.Connection.RemoteIpAddress?.ToString()
           ?? "desconhecido";
}
