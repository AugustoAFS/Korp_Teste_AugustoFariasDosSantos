using Scalar.AspNetCore;

namespace Gateway.Config;

public static class OpenApiConfig
{
    public static IServiceCollection AddDocumentation(this IServiceCollection services)
        => services.AddOpenApi("v1", options => options.AddDocumentTransformer<SecuritySchemeTransformer>());

    public static WebApplication MapDocumentation(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;

        app.MapOpenApi().AllowAnonymous();

        app.MapScalarApiReference(options => options
                .WithTitle("Emissor NF — API Gateway")
                .WithTheme(ScalarTheme.BluePlanet))
            .AllowAnonymous();

        return app;
    }
}
