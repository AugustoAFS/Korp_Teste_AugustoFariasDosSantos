using Estoque.ApplicationService.Interfaces;
using Estoque.ApplicationService.Services;
using Estoque.Domain.Dtos.EventListeners;
using Estoque.Domain.Entities;
using Estoque.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Estoque.TestesUnitarios.Services;

public sealed class StockDebitServiceTests
{
    private readonly IProductRepository _produtos = Substitute.For<IProductRepository>();
    private readonly IStockMovementRepository _movimentos = Substitute.For<IStockMovementRepository>();
    private readonly IProcessedMessageRepository _mensagens = Substitute.For<IProcessedMessageRepository>();
    private readonly IEstoqueEventPublisher _publisher = Substitute.For<IEstoqueEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly StockDebitService _service;

    private static readonly Guid ProdutoA = new("00000000-0000-0000-0000-0000000000aa");
    private static readonly Guid ProdutoB = new("00000000-0000-0000-0000-0000000000bb");

    public StockDebitServiceTests()
    {
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(true);
        _publisher.PublishStockDebited(
                Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<IReadOnlyList<UpdatedBalance>>(), Arg.Any<CancellationToken>())
            .Returns(new StoredEvent { Type = "EstoqueBaixadoEvent", Payload = "{}" });
        _publisher.PublishStockRejected(
                Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StoredEvent { Type = "EstoqueRejeitadoEvent", Payload = "{}" });

        _service = new StockDebitService(
            _produtos, _movimentos, _mensagens, _publisher, _unitOfWork, NullLogger<StockDebitService>.Instance);
    }

    private static DebitItem Item(Guid produto, int quantidade)
        => new() { ProductId = produto, Quantity = quantidade };

    private Task<DebitResult> Baixar(params DebitItem[] itens)
        => _service.DebitStock(42, Guid.CreateVersion7(), 7, itens, default);

    #region Caminho feliz

    [Fact]
    public async Task Baixa_debita_todos_os_itens_e_publica_o_evento_de_sucesso()
    {
        _produtos.Debit(ProdutoA, 2, Arg.Any<CancellationToken>()).Returns(8);
        _produtos.Debit(ProdutoB, 3, Arg.Any<CancellationToken>()).Returns(5);

        var resultado = await Baixar(Item(ProdutoA, 2), Item(ProdutoB, 3));

        resultado.Success.ShouldBeTrue();
        resultado.Items.Count.ShouldBe(2);
        await _publisher.Received(1).PublishStockDebited(
            42, Arg.Any<Guid>(), Arg.Any<IReadOnlyList<UpdatedBalance>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Baixa_bem_sucedida_grava_um_movimento_de_saida_por_item()
    {
        _produtos.Debit(ProdutoA, 2, Arg.Any<CancellationToken>()).Returns(8);
        _produtos.Debit(ProdutoB, 3, Arg.Any<CancellationToken>()).Returns(5);

        await Baixar(Item(ProdutoA, 2), Item(ProdutoB, 3));

        await _movimentos.Received(1).AddRange(
            Arg.Is<IReadOnlyList<StockMovement>>(m => m.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Baixa_bem_sucedida_guarda_o_desfecho_para_replay_futuro()
    {
        _produtos.Debit(ProdutoA, 1, Arg.Any<CancellationToken>()).Returns(9);

        await Baixar(Item(ProdutoA, 1));

        await _mensagens.Received(1).RecordOutcome(
            Arg.Any<Guid>(),
            Arg.Is<StoredEvent>(e => e.Type == "EstoqueBaixadoEvent"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Baixa_cria_savepoint_antes_de_debitar()
    {
        _produtos.Debit(ProdutoA, 1, Arg.Any<CancellationToken>()).Returns(9);

        await Baixar(Item(ProdutoA, 1));

        await _unitOfWork.Received(1).CreateSavepoint("before_debits", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Baixa_debita_em_ordem_de_produto_para_nao_travar_com_outra_transacao()
    {
        _produtos.Debit(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(1);

        await Baixar(Item(ProdutoB, 1), Item(ProdutoA, 1));

        Received.InOrder(() =>
        {
            _produtos.Debit(ProdutoA, 1, Arg.Any<CancellationToken>());
            _produtos.Debit(ProdutoB, 1, Arg.Any<CancellationToken>());
        });
    }

    #endregion

    #region Rejeição tudo-ou-nada

    [Fact]
    public async Task Item_sem_saldo_rejeita_a_nota_inteira()
    {
        _produtos.Debit(ProdutoA, 2, Arg.Any<CancellationToken>()).Returns(8);
        _produtos.Debit(ProdutoB, 99, Arg.Any<CancellationToken>()).Returns((int?)null);

        var resultado = await Baixar(Item(ProdutoA, 2), Item(ProdutoB, 99));

        resultado.Success.ShouldBeFalse();
        resultado.RejectedProductId.ShouldBe(ProdutoB);
        resultado.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Rejeicao_volta_ao_savepoint_desfazendo_as_baixas_parciais()
    {
        _produtos.Debit(ProdutoA, 2, Arg.Any<CancellationToken>()).Returns(8);
        _produtos.Debit(ProdutoB, 99, Arg.Any<CancellationToken>()).Returns((int?)null);

        await Baixar(Item(ProdutoA, 2), Item(ProdutoB, 99));

        await _unitOfWork.Received(1).RollbackToSavepoint("before_debits", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejeicao_confirma_a_transacao_preservando_o_marcador_e_o_evento()
    {
        _produtos.Debit(ProdutoA, 99, Arg.Any<CancellationToken>()).Returns((int?)null);

        await Baixar(Item(ProdutoA, 99));

        await _publisher.Received(1).PublishStockRejected(
            42, Arg.Any<Guid>(), ProdutoA, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mensagens.Received(1).RecordOutcome(
            Arg.Any<Guid>(),
            Arg.Is<StoredEvent>(e => e.Type == "EstoqueRejeitadoEvent"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejeicao_nao_grava_movimento_de_estoque()
    {
        _produtos.Debit(ProdutoA, 99, Arg.Any<CancellationToken>()).Returns((int?)null);

        await Baixar(Item(ProdutoA, 99));

        await _movimentos.DidNotReceive().AddRange(
            Arg.Any<IReadOnlyList<StockMovement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejeicao_para_no_primeiro_item_sem_saldo()
    {
        _produtos.Debit(ProdutoA, 99, Arg.Any<CancellationToken>()).Returns((int?)null);

        await Baixar(Item(ProdutoA, 99), Item(ProdutoB, 1));

        await _produtos.DidNotReceive().Debit(ProdutoB, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Duplicata reemite o desfecho

    [Fact]
    public async Task Duplicata_com_desfecho_guardado_reemite_o_evento_original()
    {
        var guardado = new StoredEvent { Type = "EstoqueBaixadoEvent", Payload = """{"NotaFiscalId":42}""" };
        _mensagens.AlreadyProcessed(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _mensagens.Outcome(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(guardado);

        var resultado = await Baixar(Item(ProdutoA, 1));

        resultado.Success.ShouldBeTrue();
        await _publisher.Received(1).Republish(guardado, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicata_nao_debita_o_estoque_de_novo()
    {
        _mensagens.AlreadyProcessed(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _mensagens.Outcome(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new StoredEvent { Type = "EstoqueBaixadoEvent", Payload = "{}" });

        await Baixar(Item(ProdutoA, 1));

        await _produtos.DidNotReceive().Debit(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicata_sem_desfecho_guardado_apenas_desfaz_sem_publicar()
    {
        _mensagens.AlreadyProcessed(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _mensagens.Outcome(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StoredEvent?)null);

        var resultado = await Baixar(Item(ProdutoA, 1));

        resultado.Success.ShouldBeTrue();
        await _publisher.DidNotReceive().Republish(Arg.Any<StoredEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().Commit(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Corrida perdida

    [Fact]
    public async Task Consumo_que_perde_a_corrida_pelo_marcador_desfaz_sem_publicar()
    {
        _mensagens.AlreadyProcessed(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await Baixar(Item(ProdutoA, 1));

        resultado.Success.ShouldBeTrue();
        resultado.Items.ShouldBeEmpty();
        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
        await _produtos.DidNotReceive().Debit(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishStockDebited(
            Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<IReadOnlyList<UpdatedBalance>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Falha inesperada

    [Fact]
    public async Task Excecao_no_meio_da_baixa_desfaz_a_transacao_e_propaga()
    {
        _produtos.Debit(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("banco fora"));

        await Should.ThrowAsync<InvalidOperationException>(() => Baixar(Item(ProdutoA, 1)));

        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().Commit(Arg.Any<CancellationToken>());
    }

    #endregion
}
