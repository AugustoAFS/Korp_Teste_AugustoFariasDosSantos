using Faturamento.ApplicationService.Interfaces;
using Faturamento.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faturamento.EventListeners.Listeners;

public sealed class OnEstoqueRejeitado(IInvoicePrintService impressao, ILogger<OnEstoqueRejeitado> log)
    : IConsumer<EstoqueRejeitadoEvent>
{
    public async Task Consume(ConsumeContext<EstoqueRejeitadoEvent> ctx)
    {
        var mensagem = ctx.Message;

        log.LogWarning(
            "Estoque rejeitado · nota {Nota} · processamento {Processamento} · produto {Produto}",
            mensagem.NotaFiscalId, mensagem.ProcessamentoId, mensagem.ProdutoId);

        await impressao.RejectInvoice(
            ctx.MessageId ?? throw new InvalidOperationException("Mensagem sem MessageId não pode ser deduplicada."),
            mensagem.NotaFiscalId,
            mensagem.ProcessamentoId,
            mensagem.Motivo,
            ctx.CancellationToken);
    }

    public sealed class Definition : ConsumerDefinition<OnEstoqueRejeitado>
    {
        public Definition()
        {
            EndpointName = "faturamento.on-estoque-rejeitado";
            ConcurrentMessageLimit = 5;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<OnEstoqueRejeitado> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.PrefetchCount = 16;
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(3));
        }
    }
}
