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
            x.AddConsumers(typeof(OnEstoqueBaixado).Assembly);

            x.UsingRabbitMq((ctx, bus) =>
            {
                bus.Host(new Uri(cfg.GetConnectionString("RabbitMq")
                    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq não configurada.")));

                bus.MessageTopology.SetEntityNameFormatter(new UrnExchangeNameFormatter());

                bus.ConfigureEndpoints(ctx);
            });
        });

        services.AddHostedService<OutboxDispatcherWorker>();
        services.AddHostedService<PrintExpirationWorker>();

        return services;
    }
}
