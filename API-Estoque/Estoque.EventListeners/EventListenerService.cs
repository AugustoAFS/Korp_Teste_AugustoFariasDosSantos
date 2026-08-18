using Estoque.ApplicationService.Interfaces;
using Estoque.EventListeners.Listeners;
using Estoque.EventListeners.Publishers;
using Estoque.EventListeners.Workers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estoque.EventListeners;

public static class EventListenerService
{
    public static IServiceCollection AddEventListeners(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddScoped<IEstoqueEventPublisher, EstoqueEventPublisher>();

        services.AddMassTransit(x =>
        {
            #region [ Event Listeners ]

            x.AddConsumer<OnBaixarEstoque, OnBaixarEstoque.Definition>();

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

        #endregion [ Workers ]

        return services;
    }
}
