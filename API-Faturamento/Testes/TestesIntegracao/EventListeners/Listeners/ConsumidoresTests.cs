using Faturamento.ApplicationService.Interfaces;
using Faturamento.EventListeners.Listeners;
using Faturamento.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Faturamento.TestesIntegracao.EventListeners.Listeners;

public sealed class ConsumidoresTests
{
    private readonly IInvoicePrintService _impressao = Substitute.For<IInvoicePrintService>();
    private readonly IProductReplicationService _replicacao = Substitute.For<IProductReplicationService>();

    private static ConsumeContext<T> Contexto<T>(T mensagem, Guid? messageId) where T : class
    {
        var contexto = Substitute.For<ConsumeContext<T>>();
        contexto.Message.Returns(mensagem);
        contexto.MessageId.Returns(messageId);
        contexto.CancellationToken.Returns(CancellationToken.None);
        return contexto;
    }

    #region Estoque baixado

    [Fact]
    public async Task Estoque_baixado_fecha_a_nota_do_processamento_informado()
    {
        var processamento = Guid.CreateVersion7();
        var mensagem = Guid.CreateVersion7();
        var consumidor = new OnEstoqueBaixado(_impressao, NullLogger<OnEstoqueBaixado>.Instance);

        await consumidor.Consume(Contexto(
            new EstoqueBaixadoEvent { NotaFiscalId = 42, ProcessamentoId = processamento, Itens = [] },
            mensagem));

        await _impressao.Received(1).CloseInvoice(mensagem, 42, processamento, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mensagem_sem_MessageId_e_recusada_porque_nao_da_para_deduplicar()
    {
        var consumidor = new OnEstoqueBaixado(_impressao, NullLogger<OnEstoqueBaixado>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() => consumidor.Consume(Contexto(
            new EstoqueBaixadoEvent { NotaFiscalId = 42, ProcessamentoId = Guid.CreateVersion7(), Itens = [] },
            messageId: null)));
    }

    #endregion

    #region Estoque rejeitado

    [Fact]
    public async Task Estoque_rejeitado_repassa_o_motivo_para_o_usuario()
    {
        var processamento = Guid.CreateVersion7();
        var mensagem = Guid.CreateVersion7();
        var consumidor = new OnEstoqueRejeitado(_impressao, NullLogger<OnEstoqueRejeitado>.Instance);

        await consumidor.Consume(Contexto(
            new EstoqueRejeitadoEvent
            {
                NotaFiscalId = 42,
                ProcessamentoId = processamento,
                ProdutoId = Guid.CreateVersion7(),
                Motivo = "Saldo insuficiente."
            },
            mensagem));

        await _impressao.Received(1).RejectInvoice(
            mensagem, 42, processamento, "Saldo insuficiente.", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Replicação do catálogo

    [Fact]
    public async Task Produto_criado_e_replicado_com_o_tipo_da_mensagem()
    {
        var produto = Guid.CreateVersion7();
        var mensagem = Guid.CreateVersion7();
        var consumidor = new OnProdutoCriado(_replicacao, NullLogger<OnProdutoCriado>.Instance);

        await consumidor.Consume(Contexto(
            new ProdutoCriadoEvent
            {
                ProdutoId = produto,
                Codigo = "PAR-M8",
                Descricao = "Parafuso sextavado M8",
                Ativo = true,
                AtualizadoEm = DateTimeOffset.UtcNow
            },
            mensagem));

        await _replicacao.Received(1).Replicate(
            mensagem, nameof(ProdutoCriadoEvent), produto, "PAR-M8", "Parafuso sextavado M8", true,
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Produto_atualizado_leva_a_situacao_para_o_catalogo()
    {
        var produto = Guid.CreateVersion7();
        var consumidor = new OnProdutoAtualizado(_replicacao, NullLogger<OnProdutoAtualizado>.Instance);

        await consumidor.Consume(Contexto(
            new ProdutoAtualizadoEvent
            {
                ProdutoId = produto,
                Codigo = "PAR-M8",
                Descricao = "Parafuso",
                Ativo = false,
                AtualizadoEm = DateTimeOffset.UtcNow
            },
            Guid.CreateVersion7()));

        await _replicacao.Received(1).Replicate(
            Arg.Any<Guid>(), nameof(ProdutoAtualizadoEvent), produto, Arg.Any<string>(), Arg.Any<string>(),
            false, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Definição dos endpoints

    [Theory]
    [InlineData(typeof(OnEstoqueBaixado.Definition), "faturamento.on-estoque-baixado")]
    [InlineData(typeof(OnEstoqueRejeitado.Definition), "faturamento.on-estoque-rejeitado")]
    [InlineData(typeof(OnProdutoCriado.Definition), "faturamento.on-produto-criado")]
    [InlineData(typeof(OnProdutoAtualizado.Definition), "faturamento.on-produto-atualizado")]
    public void Cada_consumidor_tem_fila_com_nome_estavel(Type definicao, string esperado)
    {
        var instancia = (IConsumerDefinition)Activator.CreateInstance(definicao)!;

        instancia.GetEndpointName(DefaultEndpointNameFormatter.Instance).ShouldBe(esperado);
    }

    [Fact]
    public void Replicacao_do_catalogo_e_serializada_para_preservar_a_ordem_dos_eventos()
    {
        new OnProdutoCriado.Definition().ConcurrentMessageLimit.ShouldBe(1);
        new OnProdutoAtualizado.Definition().ConcurrentMessageLimit.ShouldBe(1);
    }

    [Fact]
    public void Desfecho_do_saga_processa_em_paralelo_porque_a_dedup_resolve()
    {
        new OnEstoqueBaixado.Definition().ConcurrentMessageLimit.ShouldBe(5);
        new OnEstoqueRejeitado.Definition().ConcurrentMessageLimit.ShouldBe(5);
    }

    #endregion
}
