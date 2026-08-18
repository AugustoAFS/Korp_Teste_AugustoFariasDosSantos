using Faturamento.Ai.Abstractions;
using Faturamento.Ai.Features;
using Faturamento.Domain.Dtos.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Faturamento.TestesUnitarios.Ai;

public sealed class InvoiceItemInterpreterTests
{
    private readonly IChatModel _modelo = Substitute.For<IChatModel>();
    private readonly InvoiceItemInterpreter _interpreter;

    private static readonly IReadOnlyList<CatalogEntry> Catalogo =
    [
        new() { Code = "PAR-M8", Description = "Parafuso sextavado M8" },
        new() { Code = "MAR-BOR", Description = "Martelo de borracha" }
    ];

    public InvoiceItemInterpreterTests()
    {
        _modelo.Enabled.Returns(true);
        _interpreter = new InvoiceItemInterpreter(_modelo, NullLogger<InvoiceItemInterpreter>.Instance);
    }

    private void Responde(string? texto)
        => _modelo.Complete(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>()).Returns(texto);

    private Task<IReadOnlyList<ParsedItem>> Interpretar(string frase = "3 parafusos")
        => _interpreter.Interpret(frase, Catalogo, default);

    #region A feature não depende de fornecedor

    [Fact]
    public async Task Resposta_valida_vira_itens_independente_de_qual_modelo_respondeu()
    {
        Responde("""{"itens":[{"codigo":"PAR-M8","quantidade":3}]}""");

        var itens = await Interpretar();

        itens.Single().Code.ShouldBe("PAR-M8");
        itens.Single().Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Modelo_desligado_nao_e_chamado()
    {
        _modelo.Enabled.Returns(false);

        (await Interpretar()).ShouldBeEmpty();

        await _modelo.DidNotReceive().Complete(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Catalogo_vazio_nao_gasta_chamada()
    {
        await _interpreter.Interpret("3 parafusos", [], default);

        await _modelo.DidNotReceive().Complete(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task O_catalogo_e_o_pedido_chegam_no_prompt()
    {
        Responde("""{"itens":[]}""");

        await Interpretar("3 parafusos sextavados");

        await _modelo.Received(1).Complete(
            Arg.Is<ChatRequest>(pedido =>
                pedido.Prompt.Contains("PAR-M8")
                && pedido.Prompt.Contains("3 parafusos sextavados")
                && pedido.JsonSchema != null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Tolerância a modelo mal-comportado

    [Fact]
    public async Task Resposta_vazia_devolve_lista_vazia_sem_explodir()
    {
        Responde(null);

        (await Interpretar()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Json_invalido_devolve_lista_vazia_sem_explodir()
    {
        Responde("isso não é json");

        (await Interpretar()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Modelo_que_embrulha_o_json_em_texto_ainda_e_entendido()
    {
        Responde("""Claro! Aqui está: {"itens":[{"codigo":"PAR-M8","quantidade":2}]} Espero ter ajudado.""");

        (await Interpretar()).Single().Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task Item_sem_codigo_e_descartado()
    {
        Responde("""{"itens":[{"codigo":"","quantidade":2},{"codigo":"PAR-M8","quantidade":1}]}""");

        (await Interpretar()).Single().Code.ShouldBe("PAR-M8");
    }

    [Fact]
    public async Task Quantidade_zero_ou_negativa_e_descartada()
    {
        Responde("""{"itens":[{"codigo":"PAR-M8","quantidade":0},{"codigo":"MAR-BOR","quantidade":-3}]}""");

        (await Interpretar()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Resposta_sem_a_chave_itens_devolve_vazio()
    {
        Responde("""{"produtos":[{"codigo":"PAR-M8","quantidade":1}]}""");

        (await Interpretar()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Falha_do_fornecedor_sobe_para_o_servico_decidir()
    {
        _modelo.Complete(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("fornecedor fora"));

        await Should.ThrowAsync<HttpRequestException>(() => Interpretar());
    }

    #endregion
}
