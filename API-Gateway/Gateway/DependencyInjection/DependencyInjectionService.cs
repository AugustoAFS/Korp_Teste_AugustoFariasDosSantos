using Gateway.Config;
using Gateway.Data;
using Gateway.Security;
using Gateway.Security.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gateway.DependencyInjection;

public static class DependencyInjectionService
{
    private const int MinimumKeyLength = 32;
    private const int MinimumPepperLength = 16;

    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GatewayDbContext>(options => options
            .UseNpgsql(RequiredSetting.Of(configuration, "ConnectionStrings:GatewayDb", 1))
            .UseSnakeCaseNamingConvention());

        services.AddHttpContextAccessor();
        services.AddProblemDetails();

        services.AddSingleton<IArgon2idHasher>(
            new Argon2idHasher(RequiredSetting.Of(configuration, "Security:Pepper", MinimumPepperLength)));

        services.AddSingleton<ITokenService>(
            new TokenService(RequiredSetting.Of(configuration, "Security:JwtKey", MinimumKeyLength)));

        services.AddScopedByConvention();

        return services;
    }
}
