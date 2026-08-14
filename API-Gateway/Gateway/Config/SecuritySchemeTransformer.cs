using Gateway.Middleware;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Gateway.Config;

public sealed class SecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[AuthConfig.SessionCookie] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = AuthConfig.SessionCookie,
            Description = "Cookie de sessão emitido por POST /api/v1/auth/login."
        };

        document.Components.SecuritySchemes[AntiforgeryMiddleware.TokenHeader] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = AntiforgeryMiddleware.TokenHeader,
            Description = $"Valor do cookie {AntiforgeryMiddleware.TokenCookie}, exigido em requisições inseguras autenticadas."
        };

        return Task.CompletedTask;
    }
}
