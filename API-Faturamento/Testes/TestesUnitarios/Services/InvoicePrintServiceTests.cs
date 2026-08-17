using Faturamento.ApplicationService.Interfaces;
using Faturamento.ApplicationService.Services;
using Faturamento.Domain.Dtos.EventListeners;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Faturamento.TestesUnitarios.Services;

public sealed class InvoicePrintServiceTests
{
    private readonly IInvoiceRepository _notas = Substitute.For<IInvoiceRepository>();
    private readonly IProcessedMessageRepository _mensagens = Substitute.For<IProcessedMessageRepository>();
    private readonly IFaturamentoEventPublisher _publisher = Substitute.For<IFaturamentoEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly InvoicePrintService _service;

    private static readonly Guid Produto = Guid.CreateVersion7();

    public InvoicePrintServiceTests()
    {
        _usuario.Id.Returns(7L);
        _usuario.SeesEveryInvoice.Returns(false);
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(true);
        _notas.StartPrinting(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _notas.RestartPrinting(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        _service = new InvoicePrintService(
            _notas, _mensagens, _publisher, _unitOfWork, _usuario, NullLogger<InvoicePrintService>.Instance);
    }

    private static Invoice NotaComItem(long emitidaPor = 7)
    {
        var nota = new Invoice(1, emitidaPor, "Augusto");
        nota.AddItem(Produto, "PAR-M8", "Parafuso sextavado M8", 2);
        return nota;
    }

    private void Existe(Invoice nota) => _notas.GetById(1, Arg.Any<CancellationToken>()).Returns(nota);

    private void EmProcessamento(Invoice nota, Guid processamento)
        => _notas.GetByProcessing(1, processamento, Arg.Any<CancellationToken>()).Returns(nota);

    #region Recusas antes de imprimir

    [Fact]
    public async Task Nota_inexistente_devolve_404()
    {
        _notas.GetById(1, Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        (await _service.PrintInvoice(1, default)).Error!.Code.ShouldBe("invoice_not_found");
    }

    [Fact]
    public async Task Nota_de_outro_usuario_devolve_404_e_nao_403()
    {
        Existe(NotaComItem(emitidaPor: 99));

        (await _service.PrintInvoice(1, default)).Error!.Code.ShouldBe("invoice_not_found");
    }

    [Fact]
    public async Task Nota_ja_impressa_nao_pode_ser_reimpressa()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());
        nota.Close();
        Existe(nota);

        (await _service.PrintInvoice(1, default)).Error!.Code.ShouldBe("invoice_already_closed");
    }

    [Fact]
    public async Task Nota_ja_imprimindo_nao_dispara_segunda_impressao()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());
        Existe(nota);

        (await _service.PrintInvoice(1, default)).Error!.Code.ShouldBe("invoice_already_printing");
    }

    [Fact]
    public async Task Nota_sem_item_nao_pode_ser_impressa()
    {
        Existe(new Invoice(1, 7, "Augusto"));

        (await _service.PrintInvoice(1, default)).Error!.Code.ShouldBe("invoice_empty");
    }

    [Fact]
    public async Task Nota_recusada_nao_publica_comando_de_baixa()
    {
        Existe(new Invoice(1, 7, "Augusto"));

        await _service.PrintInvoice(1, default);

        await _publisher.DidNotReceive().PublishDebitStock(
            Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<long?>(),
            Arg.Any<IReadOnlyList<DebitItem>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Impressão aceita

    [Fact]
    public async Task Impressao_valida_devolve_202_accepted()
    {
        Existe(NotaComItem());

        var resultado = await _service.PrintInvoice(1, default);

        resultado.Success.ShouldBeTrue();
        resultado.Status.ShouldBe(System.Net.HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Impressao_publica_o_comando_com_os_itens_da_nota()
    {
        Existe(NotaComItem());

        await _service.PrintInvoice(1, default);

        await _publisher.Received(1).PublishDebitStock(
            1,
            Arg.Any<Guid>(),
            7L,
            Arg.Is<IReadOnlyList<DebitItem>>(itens =>
                itens.Count == 1 && itens[0].ProductId == Produto && itens[0].Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Impressao_reserva_a_nota_antes_de_publicar()
    {
        Existe(NotaComItem());

        await _service.PrintInvoice(1, default);

        await _notas.Received(1).StartPrinting(1, Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserva_perdida_para_outra_requisicao_devolve_already_printing()
    {
        Existe(NotaComItem());
        _notas.StartPrinting(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await _service.PrintInvoice(1, default);

        resultado.Error!.Code.ShouldBe("invoice_already_printing");
        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Retentativa reaproveita o processamento

    [Fact]
    public async Task Retentativa_de_nota_expirada_republica_sob_a_mesma_chave()
    {
        var nota = NotaComItem();
        var processamento = Guid.CreateVersion7();
        nota.StartPrinting(processamento);
        nota.ExpirePrinting("expirou");
        Existe(nota);

        await _service.PrintInvoice(1, default);

        await _notas.Received(1).RestartPrinting(1, processamento, Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishDebitStock(
            1, processamento, Arg.Any<long?>(),
            Arg.Any<IReadOnlyList<DebitItem>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retentativa_nao_usa_o_caminho_de_primeira_impressao()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());
        nota.ExpirePrinting("expirou");
        Existe(nota);

        await _service.PrintInvoice(1, default);

        await _notas.DidNotReceive().StartPrinting(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Consumo do desfecho

    [Fact]
    public async Task Estoque_baixado_fecha_a_nota()
    {
        var nota = NotaComItem();
        var processamento = Guid.CreateVersion7();
        nota.StartPrinting(processamento);
        EmProcessamento(nota, processamento);

        await _service.CloseInvoice(Guid.CreateVersion7(), 1, processamento, default);

        nota.Status.ShouldBe(Domain.Enums.InvoiceStatus.Closed);
        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Estoque_rejeitado_devolve_a_nota_para_aberta_com_o_motivo()
    {
        var nota = NotaComItem();
        var processamento = Guid.CreateVersion7();
        nota.StartPrinting(processamento);
        EmProcessamento(nota, processamento);

        await _service.RejectInvoice(Guid.CreateVersion7(), 1, processamento, "Saldo insuficiente.", default);

        nota.Status.ShouldBe(Domain.Enums.InvoiceStatus.Open);
        nota.LastError.ShouldBe("Saldo insuficiente.");
    }

    [Fact]
    public async Task Mensagem_ja_processada_e_ignorada()
    {
        _mensagens.AlreadyProcessed(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        await _service.CloseInvoice(Guid.CreateVersion7(), 1, Guid.CreateVersion7(), default);

        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
        await _notas.DidNotReceive().GetByProcessing(
            Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consumo_concorrente_da_mesma_mensagem_e_abandonado()
    {
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(false);

        await _service.CloseInvoice(Guid.CreateVersion7(), 1, Guid.CreateVersion7(), default);

        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resultado_de_processamento_antigo_e_descartado_sem_alterar_a_nota()
    {
        _notas.GetByProcessing(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        await _service.CloseInvoice(Guid.CreateVersion7(), 1, Guid.CreateVersion7(), default);

        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Expiração

    [Fact]
    public async Task Expiracao_sem_nota_pendente_nao_faz_nada()
    {
        _notas.Expired(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Invoice>)[]);

        (await _service.ExpirePrintings(TimeSpan.FromSeconds(60), 50, default)).ShouldBe(0);
        await _unitOfWork.DidNotReceive().SaveWithoutConflict(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Expiracao_marca_o_erro_mas_preserva_o_processamento()
    {
        var nota = NotaComItem();
        var processamento = Guid.CreateVersion7();
        nota.StartPrinting(processamento);

        _notas.Expired(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Invoice>)[nota]);

        var expiradas = await _service.ExpirePrintings(TimeSpan.FromSeconds(60), 50, default);

        expiradas.ShouldBe(1);
        nota.LastError.ShouldNotBeNullOrWhiteSpace();
        nota.ProcessingId.ShouldBe(processamento);
    }

    [Fact]
    public async Task Mensagem_de_expiracao_orienta_o_usuario_a_tentar_de_novo()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());

        _notas.Expired(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Invoice>)[nota]);

        await _service.ExpirePrintings(TimeSpan.FromSeconds(60), 50, default);

        nota.LastError.ShouldContain("novamente");
    }

    #endregion
}
