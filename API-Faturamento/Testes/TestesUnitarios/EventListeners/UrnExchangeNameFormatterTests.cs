using Faturamento.EventListeners;
using Faturamento.EventListeners.Messages.Consumidos;
using Faturamento.EventListeners.Messages.Publicados;
using Shouldly;

namespace Faturamento.TestesUnitarios.EventListeners;

public sealed class UrnExchangeNameFormatterTests
{
    private readonly UrnExchangeNameFormatter _formatter = new();

    [Fact]
    public void Comando_publicado_vira_exchange_com_o_nome_da_urn()
        => _formatter.FormatEntityName<BaixarEstoqueCommand>().ShouldBe("emissor:baixar-estoque");

    [Fact]
    public void Estoque_baixado_consumido_usa_a_mesma_exchange_do_estoque()
        => _formatter.FormatEntityName<EstoqueBaixadoEvent>().ShouldBe("emissor:estoque-baixado");

    [Fact]
    public void Estoque_rejeitado_consumido_usa_a_mesma_exchange_do_estoque()
        => _formatter.FormatEntityName<EstoqueRejeitadoEvent>().ShouldBe("emissor:estoque-rejeitado");

    [Fact]
    public void Produto_criado_consumido_usa_a_mesma_exchange_do_estoque()
        => _formatter.FormatEntityName<ProdutoCriadoEvent>().ShouldBe("emissor:produto-criado");

    [Fact]
    public void Produto_atualizado_consumido_usa_a_mesma_exchange_do_estoque()
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
        var nome = _formatter.FormatEntityName<EstoqueBaixadoEvent>();

        nome.ShouldNotContain("Faturamento.EventListeners");
        nome.ShouldNotContain(nameof(EstoqueBaixadoEvent));
    }
}
