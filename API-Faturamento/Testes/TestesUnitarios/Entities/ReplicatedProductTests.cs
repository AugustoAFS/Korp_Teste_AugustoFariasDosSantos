using Faturamento.Domain.Entities;
using Shouldly;

namespace Faturamento.TestesUnitarios.Entities;

public sealed class ReplicatedProductTests
{
    [Fact]
    public void Replica_guarda_o_catalogo_vindo_do_estoque()
    {
        var produtoId = Guid.CreateVersion7();
        var momento = DateTimeOffset.UtcNow;

        var replica = new ReplicatedProduct(produtoId, "PAR-M8", "Parafuso sextavado M8", true, momento);

        replica.ProductId.ShouldBe(produtoId);
        replica.Code.ShouldBe("PAR-M8");
        replica.Description.ShouldBe("Parafuso sextavado M8");
        replica.Active.ShouldBeTrue();
        replica.UpdatedAt.ShouldBe(momento);
    }

    [Fact]
    public void Replica_carrega_a_situacao_do_produto_para_bloquear_inclusao_de_inativo()
    {
        var replica = new ReplicatedProduct(
            Guid.CreateVersion7(), "PAR-M8", "Parafuso", false, DateTimeOffset.UtcNow);

        replica.Active.ShouldBeFalse();
    }

    [Fact]
    public void Replica_nao_guarda_saldo_porque_o_estoque_e_a_autoridade()
        => typeof(ReplicatedProduct)
            .GetProperties()
            .Select(propriedade => propriedade.Name)
            .ShouldNotContain("Balance");
}
