using Faturamento.Domain.Entities;
using Shouldly;

namespace Faturamento.TestesUnitarios.Entities;

public sealed class InvoiceItemTests
{
    private static InvoiceItem Item(int quantidade = 2)
        => new(Guid.CreateVersion7(), "PAR-M8", "Parafuso sextavado M8", quantidade);

    [Fact]
    public void Item_guarda_o_snapshot_do_produto()
    {
        var produtoId = Guid.CreateVersion7();

        var item = new InvoiceItem(produtoId, "PAR-M8", "Parafuso sextavado M8", 3);

        item.ProductId.ShouldBe(produtoId);
        item.ProductCode.ShouldBe("PAR-M8");
        item.ProductDescription.ShouldBe("Parafuso sextavado M8");
        item.Quantity.ShouldBe(3);
    }

    [Fact]
    public void ChangeQuantity_altera_apenas_a_quantidade()
    {
        var item = Item();

        item.ChangeQuantity(10);

        item.Quantity.ShouldBe(10);
        item.ProductCode.ShouldBe("PAR-M8");
        item.ProductDescription.ShouldBe("Parafuso sextavado M8");
    }

    [Fact]
    public void Snapshot_nao_muda_quando_o_produto_de_origem_e_renomeado()
    {
        var item = Item();

        item.ChangeQuantity(5);

        item.ProductDescription.ShouldBe("Parafuso sextavado M8");
    }
}
