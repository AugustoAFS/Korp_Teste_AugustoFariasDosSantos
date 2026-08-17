using Estoque.ApplicationService.Interfaces;
using Estoque.EventListeners.Listeners;
using Estoque.EventListeners.Workers;
using Estoque.TestesIntegracao.Suporte;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Estoque.TestesIntegracao.EventListeners;

[Collection(BancoCollection.Nome)]
public sealed class EventListenerServiceTests : IDisposable
{
    private readonly EstoqueApiFactory _api;

    public EventListenerServiceTests(SqlServerFixture banco, RabbitMqFixture broker)
        => _api = new EstoqueApiFactory(banco, broker);

    public void Dispose() => _api.Dispose();

    private IServiceProvider Servicos()
    {
        using var _ = _api.CreateClient();
        return _api.Services;
    }

    [Fact]
    public void Bus_do_MassTransit_e_registrado()
        => Servicos().GetService<IBus>().ShouldNotBeNull();

    [Fact]
    public void Consumidor_e_descoberto_pela_varredura_do_assembly()
    {
        using var escopo = Servicos().CreateScope();

        escopo.ServiceProvider.GetService<OnBaixarEstoque>().ShouldNotBeNull();
    }

    [Fact]
    public void Publisher_de_eventos_e_registrado_por_escopo()
    {
        using var escopo = Servicos().CreateScope();

        escopo.ServiceProvider.GetService<IEstoqueEventPublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void Dispatcher_do_outbox_sobe_como_hosted_service()
        => Servicos().GetServices<IHostedService>()
            .ShouldContain(servico => servico is OutboxDispatcherWorker);

    [Fact]
    public void Bus_do_MassTransit_tambem_sobe_como_hosted_service()
        => Servicos().GetServices<IHostedService>().ShouldNotBeEmpty();
}
