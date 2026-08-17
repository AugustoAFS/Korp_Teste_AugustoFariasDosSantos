using Faturamento.ApplicationService.Services;
using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Faturamento.TestesUnitarios.Services;

public sealed class InvoiceServiceTests
{
    private readonly IInvoiceRepository _notas = Substitute.For<IInvoiceRepository>();
    private readonly IReplicatedProductRepository _produtos = Substitute.For<IReplicatedProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly InvoiceService _service;

    private static readonly Guid Produto = Guid.CreateVersion7();

    public InvoiceServiceTests()
    {
        _usuario.Id.Returns(7L);
        _usuario.Name.Returns("Augusto");
        _usuario.SeesEveryInvoice.Returns(false);
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(true);

        _service = new InvoiceService(
            _notas, _produtos, _unitOfWork, _usuario, NullLogger<InvoiceService>.Instance);
    }

    private static Invoice Nota(long emitidaPor = 7) => new(1, emitidaPor, "Augusto");

    private void Existe(Invoice nota) => _notas.GetById(1, Arg.Any<CancellationToken>()).Returns(nota);

    private void ProdutoReplicado(bool ativo = true)
        => _produtos.GetById(Produto, Arg.Any<CancellationToken>())
            .Returns(new ReplicatedProduct(Produto, "PAR-M8", "Parafuso sextavado M8", ativo, DateTimeOffset.UtcNow));

    #region Visibilidade

    [Fact]
    public async Task Usuario_comum_nao_enxerga_nota_de_outro_e_recebe_404_e_nao_403()
    {
        Existe(Nota(emitidaPor: 99));

        var resultado = await _service.GetInvoiceById(1, default);

        resultado.Error!.Code.ShouldBe("invoice_not_found");
    }

    [Fact]
    public async Task Gerente_enxerga_nota_de_qualquer_um()
    {
        _usuario.SeesEveryInvoice.Returns(true);
        Existe(Nota(emitidaPor: 99));

        var resultado = await _service.GetInvoiceById(1, default);

        resultado.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Listagem_de_usuario_comum_filtra_pelo_proprio_id()
    {
        _notas.GetPaged(Arg.Any<InvoiceFilterRequest>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Invoice>)[], 0));

        await _service.GetInvoices(new InvoiceFilterRequest(), default);

        await _notas.Received(1).GetPaged(Arg.Any<InvoiceFilterRequest>(), 7L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listagem_de_gerente_nao_filtra_por_usuario()
    {
        _usuario.SeesEveryInvoice.Returns(true);
        _notas.GetPaged(Arg.Any<InvoiceFilterRequest>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Invoice>)[], 0));

        await _service.GetInvoices(new InvoiceFilterRequest(), default);

        await _notas.Received(1).GetPaged(Arg.Any<InvoiceFilterRequest>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sem_sessao_a_listagem_e_recusada()
    {
        _usuario.Id.Returns((long?)null);

        var resultado = await _service.GetInvoices(new InvoiceFilterRequest(), default);

        resultado.Error!.Code.ShouldBe("invalid_session");
    }

    #endregion

    #region Criação

    [Fact]
    public async Task Nota_nova_recebe_o_proximo_numero_sequencial()
    {
        _notas.NextNumber(Arg.Any<CancellationToken>()).Returns(42L);

        var resultado = await _service.CreateInvoice(default);

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Number.ShouldBe(42);
        resultado.Status.ShouldBe(System.Net.HttpStatusCode.Created);
    }

    [Fact]
    public async Task Nota_nova_nasce_aberta_e_no_nome_de_quem_emitiu()
    {
        _notas.NextNumber(Arg.Any<CancellationToken>()).Returns(1L);

        var resultado = await _service.CreateInvoice(default);

        resultado.Value!.Status.ShouldBe(Domain.Enums.InvoiceStatus.Open);
        resultado.Value.IssuedByUserName.ShouldBe("Augusto");
    }

    #endregion

    #region Inclusão de item

    [Fact]
    public async Task Inclusao_em_nota_inexistente_devolve_404()
    {
        _notas.GetById(1, Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Produto, Quantity = 1 }, default);

        resultado.Error!.Code.ShouldBe("invoice_not_found");
    }

    [Fact]
    public async Task Inclusao_em_nota_fechada_e_recusada()
    {
        var nota = Nota();
        nota.AddItem(Produto, "PAR-M8", "Parafuso", 1);
        nota.StartPrinting(Guid.CreateVersion7());
        nota.Close();
        Existe(nota);

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Guid.CreateVersion7(), Quantity = 1 }, default);

        resultado.Error!.Code.ShouldBe("invoice_already_closed");
    }

    [Fact]
    public async Task Inclusao_em_nota_imprimindo_e_recusada()
    {
        var nota = Nota();
        nota.StartPrinting(Guid.CreateVersion7());
        Existe(nota);

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Produto, Quantity = 1 }, default);

        resultado.Error!.Code.ShouldBe("invoice_already_printing");
    }

    [Fact]
    public async Task Produto_ainda_nao_replicado_devolve_product_not_found()
    {
        Existe(Nota());
        _produtos.GetById(Produto, Arg.Any<CancellationToken>()).Returns((ReplicatedProduct?)null);

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Produto, Quantity = 1 }, default);

        resultado.Error!.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task Produto_inativo_nao_entra_na_nota()
    {
        Existe(Nota());
        ProdutoReplicado(ativo: false);

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Produto, Quantity = 1 }, default);

        resultado.Error!.Code.ShouldBe("product_inactive");
    }

    [Fact]
    public async Task Mesmo_produto_duas_vezes_e_recusado()
    {
        var nota = Nota();
        nota.AddItem(Produto, "PAR-M8", "Parafuso", 1);
        Existe(nota);
        ProdutoReplicado();

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Produto, Quantity = 1 }, default);

        resultado.Error!.Code.ShouldBe("invoice_item_duplicated");
    }

    [Fact]
    public async Task Inclusao_valida_copia_o_snapshot_do_produto_e_devolve_a_nota_inteira()
    {
        Existe(Nota());
        ProdutoReplicado();

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Produto, Quantity = 3 }, default);

        resultado.Success.ShouldBeTrue();
        resultado.Status.ShouldBe(System.Net.HttpStatusCode.Created);

        var item = resultado.Value!.Items.Single();
        item.ProductCode.ShouldBe("PAR-M8");
        item.Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Corrida_pela_inclusao_do_mesmo_produto_e_resolvida_como_duplicata()
    {
        Existe(Nota());
        ProdutoReplicado();
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await _service.AddInvoiceItem(
            1, new AddInvoiceItemRequest { ProductId = Produto, Quantity = 1 }, default);

        resultado.Error!.Code.ShouldBe("invoice_item_duplicated");
    }

    #endregion

    #region Alteração e remoção de item

    [Fact]
    public async Task Alteracao_de_item_inexistente_devolve_invoice_item_not_found()
    {
        Existe(Nota());

        var resultado = await _service.UpdateInvoiceItem(
            1, 999, new UpdateInvoiceItemRequest { Quantity = 5 }, default);

        resultado.Error!.Code.ShouldBe("invoice_item_not_found");
    }

    [Fact]
    public async Task Remocao_de_item_inexistente_devolve_invoice_item_not_found()
    {
        Existe(Nota());

        var resultado = await _service.DeleteInvoiceItem(1, 999, default);

        resultado.Error!.Code.ShouldBe("invoice_item_not_found");
    }

    [Fact]
    public async Task Remocao_devolve_a_nota_inteira_e_nao_204()
    {
        var nota = Nota();
        nota.AddItem(Produto, "PAR-M8", "Parafuso", 1);
        Existe(nota);

        var resultado = await _service.DeleteInvoiceItem(1, nota.Items.Single().Id, default);

        resultado.Success.ShouldBeTrue();
        resultado.Status.ShouldBe(System.Net.HttpStatusCode.OK);
        resultado.Value.ShouldNotBeNull();
    }

    #endregion

    #region Exclusão da nota

    [Fact]
    public async Task Exclusao_de_nota_fechada_e_recusada()
    {
        var nota = Nota();
        nota.AddItem(Produto, "PAR-M8", "Parafuso", 1);
        nota.StartPrinting(Guid.CreateVersion7());
        nota.Close();
        Existe(nota);

        var resultado = await _service.DeleteInvoice(1, default);

        resultado.Error!.Code.ShouldBe("invoice_already_closed");
    }

    [Fact]
    public async Task Exclusao_de_nota_aberta_devolve_204()
    {
        var nota = Nota();
        Existe(nota);

        var resultado = await _service.DeleteInvoice(1, default);

        resultado.Success.ShouldBeTrue();
        resultado.Status.ShouldBe(System.Net.HttpStatusCode.NoContent);
        nota.DeletedAt.ShouldNotBeNull();
    }

    #endregion
}
