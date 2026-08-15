using System.Text;
using Faturamento.Api.Middlewares;
using Faturamento.Domain.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Faturamento.Api.Configurations;

public static class AuthConfig
{
    private const string Emissor = "emissor-gateway";
    private const string Audiencia = "emissor-servicos";
    private const int TamanhoMinimoDaChave = 32;

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var chave = configuration["Security:JwtKey"];

        if (string.IsNullOrWhiteSpace(chave))
            throw new InvalidOperationException("Security:JwtKey não configurada.");

        if (chave.Length < TamanhoMinimoDaChave)
            throw new InvalidOperationException(
                $"Security:JwtKey precisa de ao menos {TamanhoMinimoDaChave} caracteres.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Emissor,
                    ValidateAudience = true,
                    ValidAudience = Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return ProblemResponse.Write(context.HttpContext, Errors.InvalidSession).AsTask();
                    },
                    OnForbidden = context =>
                        ProblemResponse.Write(context.HttpContext, Errors.Forbidden).AsTask()
                };
            });

        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

        return services;
    }
}
