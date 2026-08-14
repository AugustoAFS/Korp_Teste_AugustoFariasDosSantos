using Gateway.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.Config;

public static class HealthCheckConfig
{
    private const string DatabaseCheck = "database";

    public static IServiceCollection AddDatabaseHealthCheck(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<GatewayDbContext>(DatabaseCheck);

        return services;
    }

    public static WebApplication MapDatabaseHealthCheck(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous()
            .DisableRateLimiting();

        app.MapHealthChecks("/health/ready")
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }
}
