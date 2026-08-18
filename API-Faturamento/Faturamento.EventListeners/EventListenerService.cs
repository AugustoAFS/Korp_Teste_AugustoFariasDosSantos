using Faturamento.ApplicationService.Interfaces;
using Faturamento.EventListeners.Listeners;
using Faturamento.EventListeners.Publishers;
using Faturamento.EventListeners.Workers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Faturamento.EventListeners;

public static class EventListenerService
{
    public static IServiceCollection AddEventListeners(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddScoped<IFaturamentoEventPublisher, FaturamentoEventPublisher>();

        services.AddMassTransit(x =>
        {
            #region [ Event Listeners ]

            x.AddConsumer<OnEstoqueBaixado, OnEstoqueBaixado.Definition>();
            x.AddConsumer<OnEstoqueRejeitado, OnEstoqueRejeitado.Definition>();
            x.AddConsumer<OnProdutoCriado, OnProdutoCriado.Definition>();
            x.AddConsumer<OnProdutoAtualizado, OnProdutoAtualizado.Definition>();

            #endregion [ Event Listeners ]

            x.UsingRabbitMq((ctx, bus) =>
            {
                bus.Host(new Uri(cfg.GetConnectionString("RabbitMq")
                    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq não configurada.")));

                bus.MessageTopology.SetEntityNameFormatter(new UrnExchangeNameFormatter());

                bus.ConfigureEndpoints(ctx);
            });
        });

        #region [ Workers ]

        services.AddHostedService<OutboxDispatcherWorker>();
        services.AddHostedService<PrintExpirationWorker>();

        #endregion [ Workers ]

        return services;
    }
}
