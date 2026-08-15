using Faturamento.ApplicationService.Interfaces;
using Faturamento.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faturamento.EventListeners.Listeners;

public sealed class OnProdutoAtualizado(IProductReplicationService replicacao, ILogger<OnProdutoAtualizado> log)
    : IConsumer<ProdutoAtualizadoEvent>
{
    public async Task Consume(ConsumeContext<ProdutoAtualizadoEvent> ctx)
    {
        var mensagem = ctx.Message;

        log.LogInformation("Produto atualizado no estoque · {Produto} · {Codigo}", mensagem.ProdutoId, mensagem.Codigo);

        await replicacao.Replicate(
            ctx.MessageId ?? throw new InvalidOperationException("Mensagem sem MessageId não pode ser deduplicada."),
            nameof(ProdutoAtualizadoEvent),
            mensagem.ProdutoId,
            mensagem.Codigo,
            mensagem.Descricao,
            mensagem.Ativo,
            mensagem.AtualizadoEm,
            ctx.CancellationToken);
    }

    public sealed class Definition : ConsumerDefinition<OnProdutoAtualizado>
    {
        public Definition()
        {
            EndpointName = "faturamento.on-produto-atualizado";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<OnProdutoAtualizado> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.PrefetchCount = 16;
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(3));
        }
    }
}
