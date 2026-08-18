using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Shouldly;

namespace Faturamento.TestesUnitarios.Entities;

public sealed class InvoiceTests
{
    private static readonly Guid Produto = Guid.CreateVersion7();

    private static Invoice Nota() => new(1, 7, "Augusto");

    private static Invoice NotaComItem()
    {
        var nota = Nota();
        nota.AddItem(Produto, "PAR-M8", "Parafuso sextavado M8", 2);
        return nota;
    }

    #region Abertura

    [Fact]
    public void Nota_nasce_aberta_e_editavel()
    {
        var nota = Nota();

        nota.Status.ShouldBe(InvoiceStatus.Open);
        nota.Editable.ShouldBeTrue();
        nota.Printing.ShouldBeFalse();
    }

    [Fact]
    public void Nota_nasce_sem_itens_sem_processamento_e_sem_erro()
    {
        var nota = Nota();

        nota.Items.ShouldBeEmpty();
        nota.ProcessingId.ShouldBeNull();
        nota.LastError.ShouldBeNull();
        nota.ClosedAt.ShouldBeNull();
    }

    [Fact]
    public void Nota_guarda_o_numero_e_quem_emitiu()
    {
        var nota = new Invoice(42, 7, "Augusto");

        nota.Number.ShouldBe(42);
        nota.IssuedByUserId.ShouldBe(7);
        nota.IssuedByUserName.ShouldBe("Augusto");
    }

    #endregion

    #region Itens

    [Fact]
    public void AddItem_copia_o_snapshot_do_produto_para_a_nota()
    {
        var nota = NotaComItem();

        var item = nota.Items.Single();
        item.ProductId.ShouldBe(Produto);
        item.ProductCode.ShouldBe("PAR-M8");
        item.ProductDescription.ShouldBe("Parafuso sextavado M8");
        item.Quantity.ShouldBe(2);
    }

    [Fact]
    public void HasProduct_acusa_produto_ja_incluido()
    {
        var nota = NotaComItem();

        nota.HasProduct(Produto).ShouldBeTrue();
        nota.HasProduct(Guid.CreateVersion7()).ShouldBeFalse();
    }

    [Fact]
    public void ItemById_devolve_nulo_quando_o_item_nao_existe()
        => NotaComItem().ItemById(999).ShouldBeNull();

    [Fact]
    public void RemoveItem_tira_o_item_da_nota()
    {
        var nota = NotaComItem();

        nota.RemoveItem(nota.Items.Single());

        nota.Items.ShouldBeEmpty();
    }

    #endregion

    #region Ciclo de impressão

    [Fact]
    public void StartPrinting_marca_a_nota_como_imprimindo()
    {
        var nota = NotaComItem();
        var processamento = Guid.CreateVersion7();

        nota.StartPrinting(processamento);

        nota.ProcessingId.ShouldBe(processamento);
        nota.ProcessingStartedAt.ShouldNotBeNull();
        nota.Printing.ShouldBeTrue();
        nota.Editable.ShouldBeFalse();
    }

    [Fact]
    public void StartPrinting_limpa_o_erro_da_tentativa_anterior()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());
        nota.ExpirePrinting("expirou");

        nota.StartPrinting(Guid.CreateVersion7());

        nota.LastError.ShouldBeNull();
        nota.Printing.ShouldBeTrue();
    }

    [Fact]
    public void Close_fecha_a_nota_e_limpa_o_processamento()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());

        nota.Close();

        nota.Status.ShouldBe(InvoiceStatus.Closed);
        nota.ClosedAt.ShouldNotBeNull();
        nota.ProcessingId.ShouldBeNull();
        nota.LastError.ShouldBeNull();
        nota.Printing.ShouldBeFalse();
    }

    [Fact]
    public void Nota_fechada_nao_e_mais_editavel()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());
        nota.Close();

        nota.Editable.ShouldBeFalse();
    }

    [Fact]
    public void Reject_devolve_a_nota_para_aberta_com_o_motivo()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());

        nota.Reject("Saldo insuficiente do produto PAR-M8.", null);

        nota.Status.ShouldBe(InvoiceStatus.Open);
        nota.ProcessingId.ShouldBeNull();
        nota.LastError.ShouldBe("Saldo insuficiente do produto PAR-M8.");
        nota.Printing.ShouldBeFalse();
    }

    [Fact]
    public void Nota_rejeitada_volta_a_ser_editavel_para_o_usuario_corrigir()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());
        nota.Reject("Saldo insuficiente.", null);

        nota.Editable.ShouldBeTrue();
    }

    #endregion

    #region Expiração preserva o processamento

    [Fact]
    public void ExpirePrinting_registra_o_erro_mas_preserva_o_processamento()
    {
        var nota = NotaComItem();
        var processamento = Guid.CreateVersion7();
        nota.StartPrinting(processamento);

        nota.ExpirePrinting("O estoque não respondeu a tempo.");

        nota.ProcessingId.ShouldBe(processamento);
        nota.LastError.ShouldBe("O estoque não respondeu a tempo.");
    }

    [Fact]
    public void Nota_expirada_deixa_de_contar_como_imprimindo()
    {
        var nota = NotaComItem();
        nota.StartPrinting(Guid.CreateVersion7());

        nota.ExpirePrinting("expirou");

        nota.Printing.ShouldBeFalse();
    }

    [Fact]
    public void Resultado_atrasado_ainda_fecha_a_nota_expirada()
    {
        var nota = NotaComItem();
        var processamento = Guid.CreateVersion7();
        nota.StartPrinting(processamento);
        nota.ExpirePrinting("expirou");

        nota.ProcessingId.ShouldBe(processamento);

        nota.Close();

        nota.Status.ShouldBe(InvoiceStatus.Closed);
        nota.LastError.ShouldBeNull();
    }

    #endregion

    #region Exclusão

    [Fact]
    public void Delete_marca_a_data_de_exclusao()
    {
        var nota = Nota();

        nota.Delete();

        nota.DeletedAt.ShouldNotBeNull();
    }

    #endregion
}
