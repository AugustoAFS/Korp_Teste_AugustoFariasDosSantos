using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Estoque.ApplicationService.DependencyInjection;

public static class DependencyInjectionService
{
    private static readonly string[] Namespaces = ["Estoque.ApplicationService.Services"];

    public static IServiceCollection AddApplicationService(this IServiceCollection services)
    {
        var implementacoes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(tipo => tipo is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(tipo => Namespaces.Any(nome => tipo.Namespace?.StartsWith(nome, StringComparison.Ordinal) == true));

        foreach (var implementacao in implementacoes)
        {
            var contrato = Array.Find(implementacao.GetInterfaces(), tipo => tipo.Name == $"I{implementacao.Name}")
                ?? throw new InvalidOperationException(
                    $"{implementacao.Name} não expõe a interface I{implementacao.Name}.");

            services.AddScoped(contrato, implementacao);
        }

        return services;
    }
}
