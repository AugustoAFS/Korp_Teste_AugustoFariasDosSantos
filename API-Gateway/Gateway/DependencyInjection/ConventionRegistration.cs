namespace Gateway.DependencyInjection;

public static class ConventionRegistration
{
    private static readonly string[] Namespaces = ["Gateway.Services", "Gateway.Repositories"];

    public static IServiceCollection AddScopedByConvention(this IServiceCollection services)
    {
        var implementations = typeof(ConventionRegistration).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(type => Namespaces.Any(name => type.Namespace?.StartsWith(name, StringComparison.Ordinal) == true));

        foreach (var implementation in implementations)
        {
            var contract = Array.Find(implementation.GetInterfaces(), type => type.Name == $"I{implementation.Name}")
                ?? throw new InvalidOperationException($"{implementation.Name} não expõe a interface I{implementation.Name}.");

            services.AddScoped(contract, implementation);
        }

        return services;
    }
}
