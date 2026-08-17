using Estoque.EventListeners;
using Estoque.EventListeners.Messages.Consumidos;
using Estoque.EventListeners.Messages.Publicados;
using Shouldly;

namespace Estoque.TestesUnitarios.EventListeners;

public sealed class UrnExchangeNameFormatterTests
{
    private readonly UrnExchangeNameFormatter _formatter = new();

    [Theory]
    [InlineData("emissor:baixar-estoque")]
    public void Comando_consumido_vira_exchange_com_o_nome_da_urn(string esperado)
        => _formatter.FormatEntityName<BaixarEstoqueCommand>().ShouldBe(esperado);

    [Fact]
    public void Estoque_baixado_vira_exchange_com_o_nome_da_urn()
        => _formatter.FormatEntityName<EstoqueBaixadoEvent>().ShouldBe("emissor:estoque-baixado");

    [Fact]
    public void Estoque_rejeitado_vira_exchange_com_o_nome_da_urn()
        => _formatter.FormatEntityName<EstoqueRejeitadoEvent>().ShouldBe("emissor:estoque-rejeitado");

    [Fact]
    public void Produto_criado_vira_exchange_com_o_nome_da_urn()
        => _formatter.FormatEntityName<ProdutoCriadoEvent>().ShouldBe("emissor:produto-criado");

    [Fact]
    public void Produto_atualizado_vira_exchange_com_o_nome_da_urn()
        => _formatter.FormatEntityName<ProdutoAtualizadoEvent>().ShouldBe("emissor:produto-atualizado");

    [Fact]
    public void Nome_da_exchange_nunca_carrega_o_prefixo_urn_message()
    {
        string[] nomes =
        [
            _formatter.FormatEntityName<BaixarEstoqueCommand>(),
            _formatter.FormatEntityName<EstoqueBaixadoEvent>(),
            _formatter.FormatEntityName<EstoqueRejeitadoEvent>(),
            _formatter.FormatEntityName<ProdutoCriadoEvent>(),
            _formatter.FormatEntityName<ProdutoAtualizadoEvent>()
        ];

        nomes.ShouldAllBe(nome => !nome.StartsWith("urn:message:", StringComparison.Ordinal));
    }

    [Fact]
    public void Nome_da_exchange_nao_deriva_do_namespace_clr()
    {
        var nome = _formatter.FormatEntityName<ProdutoCriadoEvent>();

        nome.ShouldNotContain("Estoque.EventListeners");
        nome.ShouldNotContain(nameof(ProdutoCriadoEvent));
    }
}
