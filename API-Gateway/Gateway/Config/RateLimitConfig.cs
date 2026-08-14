using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Gateway.Exceptions;
using Gateway.Middleware;

namespace Gateway.Config;

public static class RateLimitConfig
{
    public const string CredentialsPolicy = "credentials";

    private const int Requests = 120;
    private const int WindowInSeconds = 60;
    private const int CredentialRequests = 5;
    private const int CredentialWindowInSeconds = 60;

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        => services.AddRateLimiter(limiter =>
        {
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                Window(context, Requests, WindowInSeconds));

            limiter.AddPolicy(CredentialsPolicy, context =>
                Window(context, CredentialRequests, CredentialWindowInSeconds));

            limiter.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)wait.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                await ProblemResponse.Write(context.HttpContext, Errors.TooManyRequests, clearResponse: false);
            };
        });

    private static RateLimitPartition<string> Window(HttpContext context, int requests, int windowInSeconds)
        => RateLimitPartition.GetFixedWindowLimiter(Partition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = requests,
            Window = TimeSpan.FromSeconds(windowInSeconds),
            QueueLimit = 0
        });

    private static string Partition(HttpContext context)
        => context.User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? context.Connection.RemoteIpAddress?.ToString()
           ?? "unknown";
}
