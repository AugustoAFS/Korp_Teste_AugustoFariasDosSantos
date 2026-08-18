using Estoque.Domain.Interfaces;
using Estoque.InfraStructure.Data;
using Estoque.InfraStructure.Repositories;
using Estoque.InfraStructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estoque.InfraStructure.DependencyInjection;

public static class DependencyInjectionService
{
    public static IServiceCollection AddInfraStructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("EstoqueDb");

        if (string.IsNullOrWhiteSpace(conexao))
            throw new InvalidOperationException("ConnectionStrings:EstoqueDb não configurada.");

        services.AddDbContext<EstoqueDbContext>(options => options
            .UseSqlServer(conexao)
            .UseSnakeCaseNamingConvention());

        services.AddHttpContextAccessor();

        #region [ Repositories ]

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        #endregion [ Repositories ]

        #region [ Security ]

        services.AddScoped<ICurrentUser, CurrentUser>();

        #endregion [ Security ]

        return services;
    }
}
