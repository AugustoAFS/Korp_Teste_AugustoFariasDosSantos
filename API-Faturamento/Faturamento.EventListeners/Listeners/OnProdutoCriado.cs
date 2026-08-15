using Faturamento.ApplicationService.Interfaces;
using Faturamento.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faturamento.EventListeners.Listeners;

public sealed class OnProdutoCriado(IProductReplicationService replicacao, ILogger<OnProdutoCriado> log)
    : IConsumer<ProdutoCriadoEvent>
{
    public async Task Consume(ConsumeContext<ProdutoCriadoEvent> ctx)
    {
        var mensagem = ctx.Message;

        log.LogInformation("Produto criado no estoque · {Produto} · {Codigo}", mensagem.ProdutoId, mensagem.Codigo);

        await replicacao.Replicate(
            ctx.MessageId ?? throw new InvalidOperationException("Mensagem sem MessageId não pode ser deduplicada."),
            nameof(ProdutoCriadoEvent),
            mensagem.ProdutoId,
            mensagem.Codigo,
            mensagem.Descricao,
            mensagem.Ativo,
            mensagem.AtualizadoEm,
            ctx.CancellationToken);
    }

    public sealed class Definition : ConsumerDefinition<OnProdutoCriado>
    {
        public Definition()
        {
            EndpointName = "faturamento.on-produto-criado";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<OnProdutoCriado> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.PrefetchCount = 16;
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(3));
        }
    }
}
