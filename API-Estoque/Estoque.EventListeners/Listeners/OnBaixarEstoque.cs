using Estoque.ApplicationService.Interfaces;
using Estoque.Domain.Dtos.EventListeners;
using Estoque.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Estoque.EventListeners.Listeners;

public sealed class OnBaixarEstoque(IStockDebitService baixa, ILogger<OnBaixarEstoque> log)
    : IConsumer<BaixarEstoqueCommand>
{
    public async Task Consume(ConsumeContext<BaixarEstoqueCommand> ctx)
    {
        var mensagem = ctx.Message;

        log.LogInformation(
            "Baixa solicitada · nota {Nota} · processamento {Processamento} · {Quantidade} itens",
            mensagem.NotaFiscalId, mensagem.ProcessamentoId, mensagem.Itens.Count);

        await baixa.DebitStock(
            mensagem.NotaFiscalId,
            mensagem.ProcessamentoId,
            mensagem.UsuarioId,
            [.. mensagem.Itens.Select(item => new DebitItem { ProductId = item.ProdutoId, Quantity = item.Quantidade })],
            ctx.CancellationToken);
    }

    public sealed class Definition : ConsumerDefinition<OnBaixarEstoque>
    {
        public Definition()
        {
            EndpointName = "estoque.on-baixar-estoque";
            ConcurrentMessageLimit = 5;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<OnBaixarEstoque> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.PrefetchCount = 16;
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(3));
        }
    }
}
