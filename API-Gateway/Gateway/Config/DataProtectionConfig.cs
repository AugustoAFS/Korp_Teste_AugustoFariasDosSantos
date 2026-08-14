using Gateway.Data;
using Microsoft.AspNetCore.DataProtection;

namespace Gateway.Config;

public static class DataProtectionConfig
{
    private const string ApplicationName = "emissor-gateway";

    public static IServiceCollection AddPersistentDataProtection(this IServiceCollection services)
    {
        services
            .AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToDbContext<GatewayDbContext>();

        return services;
    }
}
