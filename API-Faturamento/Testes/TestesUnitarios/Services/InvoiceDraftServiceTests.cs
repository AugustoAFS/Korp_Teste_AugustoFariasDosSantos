using Faturamento.ApplicationService.Services;
using Faturamento.Domain.Dtos.Ai;
using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Faturamento.TestesUnitarios.Services;

public sealed class InvoiceDraftServiceTests
{
    private readonly IInvoiceRepository _notas = Substitute.For<IInvoiceRepository>();
    private readonly IReplicatedProductRepository _produtos = Substitute.For<IReplicatedProductRepository>();
    private readonly IInvoiceItemInterpreter _interpreter = Substitute.For<IInvoiceItemInterpreter>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly InvoiceDraftService _service;

    private static readonly Guid Parafuso = Guid.CreateVersion7();
    private static readonly Guid Martelo = Guid.CreateVersion7();

    public InvoiceDraftServiceTests()
    {
        _usuario.Id.Returns(7L);
        _usuario.SeesEveryInvoice.Returns(false);
        _interpreter.Enabled.Returns(true);

        _service = new InvoiceDraftService(
            _notas, _produtos, _interpreter, _usuario, NullLogger<InvoiceDraftService>.Instance);
    }

    private static Invoice Nota(long emitidaPor = 7) => new(1, emitidaPor, "Augusto");

    private void Existe(Invoice nota) => _notas.GetById(1, Arg.Any<CancellationToken>()).Returns(nota);

    private void Catalogo(params (Guid Id, string Codigo, string Descricao)[] itens)
        => _produtos.ActiveCatalog(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ReplicatedProduct>)
            [
                .. itens.Select(item => new ReplicatedProduct(
                    item.Id, item.Codigo, item.Descricao, true, DateTimeOffset.UtcNow))
            ]);

    private void Interpreta(params (string Codigo, int Quantidade)[] itens)
        => _interpreter.Interpret(
                Arg.Any<string>(), Arg.Any<IReadOnlyList<CatalogEntry>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ParsedItem>)
            [
                .. itens.Select(item => new ParsedItem { Code = item.Codigo, Quantity = item.Quantidade })
            ]);

    private Task<Domain.Dtos.Result<InterpretationResult>> Interpretar(string frase = "3 parafusos")
        => _service.InterpretItems(1, new InterpretItemsRequest { Phrase = frase }, default);

    #region Degradação sem IA

    [Fact]
    public async Task Assistente_desligado_devolve_ai_disabled_sem_tocar_no_banco()
    {
        _interpreter.Enabled.Returns(false);

        var resultado = await Interpretar();

        resultado.Success.ShouldBeFalse();
        resultado.Error!.Code.ShouldBe("ai_disabled");
        await _notas.DidNotReceive().GetById(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falha_do_assistente_devolve_ai_unavailable_e_nao_propaga_excecao()
    {
        Existe(Nota());
        Catalogo((Parafuso, "PAR-M8", "Parafuso sextavado M8"));
        _interpreter.Interpret(
                Arg.Any<string>(), Arg.Any<IReadOnlyList<CatalogEntry>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("anthropic fora"));

        var resultado = await Interpretar();

        resultado.Error!.Code.ShouldBe("ai_unavailable");
    }

    [Fact]
    public async Task Catalogo_vazio_devolve_resultado_vazio_sem_chamar_o_modelo()
    {
        Existe(Nota());
        Catalogo();

        var resultado = await Interpretar();

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Items.ShouldBeEmpty();
        await _interpreter.DidNotReceive().Interpret(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<CatalogEntry>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Visibilidade e estado da nota

    [Fact]
    public async Task Nota_de_outro_usuario_devolve_404()
    {
        Existe(Nota(emitidaPor: 99));

        (await Interpretar()).Error!.Code.ShouldBe("invoice_not_found");
    }

    [Fact]
    public async Task Nota_fechada_nao_aceita_sugestao()
    {
        var nota = Nota();
        nota.AddItem(Parafuso, "PAR-M8", "Parafuso", 1);
        nota.StartPrinting(Guid.CreateVersion7());
        nota.Close();
        Existe(nota);

        (await Interpretar()).Error!.Code.ShouldBe("invoice_already_closed");
    }

    [Fact]
    public async Task Nota_imprimindo_nao_aceita_sugestao()
    {
        var nota = Nota();
        nota.StartPrinting(Guid.CreateVersion7());
        Existe(nota);

        (await Interpretar()).Error!.Code.ShouldBe("invoice_already_printing");
    }

    #endregion

    #region Resolução contra o catálogo

    [Fact]
    public async Task Codigo_do_catalogo_vira_item_com_o_produto_resolvido()
    {
        Existe(Nota());
        Catalogo((Parafuso, "PAR-M8", "Parafuso sextavado M8"));
        Interpreta(("PAR-M8", 3));

        var resultado = await Interpretar();

        var item = resultado.Value!.Items.Single();
        item.ProductId.ShouldBe(Parafuso);
        item.ProductCode.ShouldBe("PAR-M8");
        item.ProductDescription.ShouldBe("Parafuso sextavado M8");
        item.Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Codigo_inventado_pelo_modelo_nunca_vira_item()
    {
        Existe(Nota());
        Catalogo((Parafuso, "PAR-M8", "Parafuso sextavado M8"));
        Interpreta(("PRODUTO-QUE-NAO-EXISTE", 2));

        var resultado = await Interpretar();

        resultado.Value!.Items.ShouldBeEmpty();
        resultado.Value.Unresolved.ShouldBe(["PRODUTO-QUE-NAO-EXISTE"]);
    }

    [Fact]
    public async Task Codigo_invalido_nao_impede_os_validos_da_mesma_frase()
    {
        Existe(Nota());
        Catalogo((Parafuso, "PAR-M8", "Parafuso"), (Martelo, "MAR-BOR", "Martelo de borracha"));
        Interpreta(("PAR-M8", 3), ("INEXISTENTE", 1), ("MAR-BOR", 2));

        var resultado = await Interpretar();

        resultado.Value!.Items.Count.ShouldBe(2);
        resultado.Value.Unresolved.ShouldBe(["INEXISTENTE"]);
    }

    [Fact]
    public async Task Codigo_e_casado_sem_diferenciar_maiuscula()
    {
        Existe(Nota());
        Catalogo((Parafuso, "PAR-M8", "Parafuso"));
        Interpreta(("par-m8", 1));

        (await Interpretar()).Value!.Items.Single().ProductCode.ShouldBe("PAR-M8");
    }

    [Fact]
    public async Task Produto_repetido_pelo_modelo_entra_uma_vez_so()
    {
        Existe(Nota());
        Catalogo((Parafuso, "PAR-M8", "Parafuso"));
        Interpreta(("PAR-M8", 3), ("PAR-M8", 2));

        (await Interpretar()).Value!.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Produto_ja_na_nota_e_sinalizado_em_vez_de_escondido()
    {
        var nota = Nota();
        nota.AddItem(Parafuso, "PAR-M8", "Parafuso", 1);
        Existe(nota);
        Catalogo((Parafuso, "PAR-M8", "Parafuso"));
        Interpreta(("PAR-M8", 3));

        (await Interpretar()).Value!.Items.Single().AlreadyInInvoice.ShouldBeTrue();
    }

    [Fact]
    public async Task Modelo_recebe_o_catalogo_sem_identificador_interno()
    {
        Existe(Nota());
        Catalogo((Parafuso, "PAR-M8", "Parafuso sextavado M8"));
        Interpreta();

        await Interpretar();

        await _interpreter.Received(1).Interpret(
            Arg.Any<string>(),
            Arg.Is<IReadOnlyList<CatalogEntry>>(catalogo =>
                catalogo.Count == 1 && catalogo[0].Code == "PAR-M8"),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
