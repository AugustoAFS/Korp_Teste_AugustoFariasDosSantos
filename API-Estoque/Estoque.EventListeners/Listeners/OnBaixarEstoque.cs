using Estoque.ApplicationService.Interfaces;
using Estoque.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Estoque.EventListeners.Listeners
{
    public sealed class OnBaixarEstoque : IConsumer<BaixarEstoqueCommand>
    {
        private readonly IBaixaEstoqueService _baixa;
        private readonly IEstoqueEventPublisher _publisher;
        private readonly ILogger<OnBaixarEstoque> _log;

        public OnBaixarEstoque(
            IBaixaEstoqueService baixa,
            IEstoqueEventPublisher publisher,
            ILogger<OnBaixarEstoque> log)
        {
            _baixa = baixa;
            _publisher = publisher;
            _log = log;
        }

        public async Task Consume(ConsumeContext<BaixarEstoqueCommand> ctx)
        {
            var msg = ctx.Message;

            _log.LogInformation(
                "Baixa solicitada · nota {Nota} · processamento {Processamento} · {Qtd} itens",
                msg.NotaFiscalId, msg.ProcessamentoId, msg.Itens.Count);

            var itens = msg.Itens
                .Select(i => new ItemBaixa(i.ProdutoId, i.Quantidade))
                .ToList();

            var resultado = await _baixa.Baixar(
                msg.NotaFiscalId, msg.ProcessamentoId, msg.UsuarioId, itens, ctx.CancellationToken);


            if (!resultado.Sucesso)
            {
                await _publisher.EstoqueRejeitado(
                    msg.NotaFiscalId,
                    msg.ProcessamentoId,
                    resultado.ProdutoRejeitado!.Value,
                    resultado.Motivo!,
                    ctx.CancellationToken);
                return;
            }

            await _publisher.EstoqueBaixado(
                msg.NotaFiscalId, msg.ProcessamentoId, resultado.Itens, ctx.CancellationToken);
        }

        public sealed class Definition : ConsumerDefinition<OnBaixarEstoque>
        {
            public Definition()
            {
                EndpointName = "estoque.on-baixar-estoque";

                // 5 de propósito: serializar em 1 esconderia a disputa de saldo
                // em vez de tratá-la. A corretude vem do UPDATE condicional
                // + CHECK (Saldo >= 0) no banco.
                ConcurrentMessageLimit = 5;
            }

            protected override void ConfigureConsumer(
                IReceiveEndpointConfigurator endpointConfigurator,
                IConsumerConfigurator<OnBaixarEstoque> consumerConfigurator,
                IRegistrationContext context)
            {
                endpointConfigurator.PrefetchCount = 16;
                endpointConfigurator.UseMessageRetry(r => r.Immediate(3));
            }
        }
    }
}
