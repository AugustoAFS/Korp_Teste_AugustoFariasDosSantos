namespace Gateway.Config;

public static class CorsConfig
{
    public const string Policy = "front";

    public static IServiceCollection AddFrontCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>();

        if (origins is null || origins.Length == 0)
            throw new InvalidOperationException("Cors:Origins não configurado.");

        return services.AddCors(cors => cors.AddPolicy(Policy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
    }
}
