using Faturamento.ApplicationService.Interfaces;
using Faturamento.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faturamento.EventListeners.Listeners;

public sealed class OnEstoqueBaixado(IInvoicePrintService impressao, ILogger<OnEstoqueBaixado> log)
    : IConsumer<EstoqueBaixadoEvent>
{
    public async Task Consume(ConsumeContext<EstoqueBaixadoEvent> ctx)
    {
        var mensagem = ctx.Message;

        log.LogInformation(
            "Estoque baixado · nota {Nota} · processamento {Processamento} · {Quantidade} itens",
            mensagem.NotaFiscalId, mensagem.ProcessamentoId, mensagem.Itens.Count);

        await impressao.CloseInvoice(
            ctx.MessageId ?? throw new InvalidOperationException("Mensagem sem MessageId não pode ser deduplicada."),
            mensagem.NotaFiscalId,
            mensagem.ProcessamentoId,
            ctx.CancellationToken);
    }

    public sealed class Definition : ConsumerDefinition<OnEstoqueBaixado>
    {
        public Definition()
        {
            EndpointName = "faturamento.on-estoque-baixado";
            ConcurrentMessageLimit = 5;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<OnEstoqueBaixado> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.PrefetchCount = 16;
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(3));
        }
    }
}
