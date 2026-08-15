namespace Estoque.Api.Configurations;

public static class CorsConfig
{
    public const string Politica = "front";

    public static IServiceCollection AddFrontCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origens = configuration.GetSection("Cors:Origins").Get<string[]>();

        if (origens is null || origens.Length == 0)
            throw new InvalidOperationException("Cors:Origins não configurado.");

        return services.AddCors(cors => cors.AddPolicy(Politica, policy => policy
            .WithOrigins(origens)
            .AllowAnyHeader()
            .AllowAnyMethod()));
    }
}
