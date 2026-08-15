using System.Reflection;
using Faturamento.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Faturamento.InfraStructure.DependencyInjection;

public static class DependencyInjectionService
{
    private static readonly string[] Namespaces =
    [
        "Faturamento.InfraStructure.Repositories",
        "Faturamento.InfraStructure.Security"
    ];

    public static IServiceCollection AddInfraStructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("FaturamentoDb");

        if (string.IsNullOrWhiteSpace(conexao))
            throw new InvalidOperationException("ConnectionStrings:FaturamentoDb não configurada.");

        services.AddDbContext<FaturamentoDbContext>(options => options
            .UseNpgsql(conexao)
            .UseSnakeCaseNamingConvention());

        services.AddHttpContextAccessor();

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
