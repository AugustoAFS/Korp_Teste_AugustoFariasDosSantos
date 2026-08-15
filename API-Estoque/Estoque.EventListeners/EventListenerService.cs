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
            x.AddConsumers(typeof(OnBaixarEstoque).Assembly);

            x.UsingRabbitMq((ctx, bus) =>
            {
                bus.Host(new Uri(cfg.GetConnectionString("RabbitMq")
                    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq não configurada.")));

                bus.ConfigureEndpoints(ctx);
            });
        });

        services.AddHostedService<OutboxDispatcherWorker>();

        return services;
    }
}
