using Estoque.Domain.Entities;
using Shouldly;

namespace Estoque.TestesUnitarios.Entities;

public sealed class ProductTests
{
    [Fact]
    public void Produto_nasce_ativo_com_o_saldo_inicial()
    {
        var produto = new Product("PAR-M8", "Parafuso sextavado M8", 10);

        produto.Code.ShouldBe("PAR-M8");
        produto.Description.ShouldBe("Parafuso sextavado M8");
        produto.Balance.ShouldBe(10);
        produto.Active.ShouldBeTrue();
    }

    [Fact]
    public void Produto_nasce_com_identificador_proprio()
    {
        var primeiro = new Product("A", "Primeiro", 0);
        var segundo = new Product("B", "Segundo", 0);

        primeiro.Id.ShouldNotBe(Guid.Empty);
        segundo.Id.ShouldNotBe(Guid.Empty);
        segundo.Id.ShouldNotBe(primeiro.Id);
    }

    [Fact]
    public void Update_altera_codigo_descricao_e_situacao()
    {
        var produto = new Product("PAR-M8", "Parafuso sextavado M8", 10);

        produto.Update("PAR-M10", "Parafuso sextavado M10", false);

        produto.Code.ShouldBe("PAR-M10");
        produto.Description.ShouldBe("Parafuso sextavado M10");
        produto.Active.ShouldBeFalse();
    }

    [Fact]
    public void Update_nao_mexe_no_saldo()
    {
        var produto = new Product("PAR-M8", "Parafuso sextavado M8", 10);

        produto.Update("PAR-M10", "Parafuso sextavado M10", true);

        produto.Balance.ShouldBe(10);
    }

    [Fact]
    public void Delete_inativa_e_marca_a_data_de_exclusao()
    {
        var produto = new Product("PAR-M8", "Parafuso sextavado M8", 0);

        produto.Delete();

        produto.Active.ShouldBeFalse();
        produto.DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Produto_recem_criado_nao_esta_excluido()
    {
        var produto = new Product("PAR-M8", "Parafuso sextavado M8", 0);

        produto.DeletedAt.ShouldBeNull();
    }
}
