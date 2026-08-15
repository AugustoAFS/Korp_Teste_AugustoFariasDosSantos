using Scalar.AspNetCore;

namespace Faturamento.Api.Configurations;

public static class OpenApiConfig
{
    public static IServiceCollection AddDocumentation(this IServiceCollection services)
        => services.AddOpenApi("v1");

    public static WebApplication MapDocumentation(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;

        app.MapOpenApi().AllowAnonymous();

        app.MapScalarApiReference(options => options
                .WithTitle("Emissor NF — Serviço de Faturamento")
                .WithTheme(ScalarTheme.BluePlanet))
            .AllowAnonymous();

        return app;
    }
}
