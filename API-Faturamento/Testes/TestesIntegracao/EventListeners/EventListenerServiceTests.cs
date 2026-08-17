using Faturamento.ApplicationService.Interfaces;
using Faturamento.EventListeners.Listeners;
using Faturamento.EventListeners.Workers;
using Faturamento.TestesIntegracao.Suporte;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Faturamento.TestesIntegracao.EventListeners;

[Collection(AmbienteCollection.Nome)]
public sealed class EventListenerServiceTests : IDisposable
{
    private readonly FaturamentoApiFactory _api;

    public EventListenerServiceTests(PostgresFixture banco, RabbitMqFixture broker)
        => _api = new FaturamentoApiFactory(banco, broker);

    public void Dispose() => _api.Dispose();

    private IServiceProvider Servicos()
    {
        using var _ = _api.ClienteAnonimo();
        return _api.Services;
    }

    [Fact]
    public void Bus_do_MassTransit_e_registrado()
        => Servicos().GetService<IBus>().ShouldNotBeNull();

    [Theory]
    [InlineData(typeof(OnEstoqueBaixado))]
    [InlineData(typeof(OnEstoqueRejeitado))]
    [InlineData(typeof(OnProdutoCriado))]
    [InlineData(typeof(OnProdutoAtualizado))]
    public void Consumidor_e_descoberto_pela_varredura_do_assembly(Type consumidor)
    {
        using var escopo = Servicos().CreateScope();

        escopo.ServiceProvider.GetService(consumidor).ShouldNotBeNull();
    }

    [Fact]
    public void Publisher_de_eventos_e_registrado_por_escopo()
    {
        using var escopo = Servicos().CreateScope();

        escopo.ServiceProvider.GetService<IFaturamentoEventPublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void Dispatcher_do_outbox_sobe_como_hosted_service()
        => Servicos().GetServices<IHostedService>()
            .ShouldContain(servico => servico is OutboxDispatcherWorker);

    [Fact]
    public void Worker_de_expiracao_sobe_como_hosted_service()
        => Servicos().GetServices<IHostedService>()
            .ShouldContain(servico => servico is PrintExpirationWorker);
}
