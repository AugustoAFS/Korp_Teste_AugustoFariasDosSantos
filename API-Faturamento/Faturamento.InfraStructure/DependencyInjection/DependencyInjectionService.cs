using Faturamento.Domain.Interfaces;
using Faturamento.InfraStructure.Data;
using Faturamento.InfraStructure.Documents;
using Faturamento.InfraStructure.Repositories;
using Faturamento.InfraStructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Faturamento.InfraStructure.DependencyInjection;

public static class DependencyInjectionService
{
    public static IServiceCollection AddInfraStructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("FaturamentoDb");

        if (string.IsNullOrWhiteSpace(conexao))
            throw new InvalidOperationException("ConnectionStrings:FaturamentoDb não configurada.");

        services.AddDbContext<FaturamentoDbContext>(options => options
            .UseNpgsql(conexao)
            .UseSnakeCaseNamingConvention());

        services.AddHttpContextAccessor();

        #region [ Repositories ]

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IReplicatedProductRepository, ReplicatedProductRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        #endregion [ Repositories ]

        #region [ Documents ]

        services.AddScoped<IInvoicePdfWriter, InvoicePdfWriter>();

        #endregion [ Documents ]

        #region [ Security ]

        services.AddScoped<ICurrentUser, CurrentUser>();

        #endregion [ Security ]

        return services;
    }
}
