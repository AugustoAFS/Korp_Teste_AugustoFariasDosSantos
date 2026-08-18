using Faturamento.Ai;
using Faturamento.Ai.Providers;
using Faturamento.Domain.Dtos.Ai;
using Faturamento.Ai.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Faturamento.TestesIntegracao.Ai;

public sealed class ModeloRealTests
{
    private static readonly string? Chave = Environment.GetEnvironmentVariable("Ai__ApiKey");

    private static readonly IReadOnlyList<CatalogEntry> Catalogo =
    [
        new() { Code = "PAR-M8", Description = "Parafuso sextavado M8" },
        new() { Code = "MAR-BOR", Description = "Martelo de borracha" },
        new() { Code = "FIT-ISO", Description = "Fita isolante preta" }
    ];

    private static InvoiceItemInterpreter Interpretador()
    {
        var opcoes = new AiOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("Ai__BaseUrl")
                      ?? "https://generativelanguage.googleapis.com/v1beta/openai",
            ApiKey = Chave!,
            Model = Environment.GetEnvironmentVariable("Ai__Model") ?? "gemini-3.6-flash",
            MaxTokens = 1024,
            TimeoutSeconds = 30
        };

        return new InvoiceItemInterpreter(
            new ChatCompletionsModel(opcoes, new HttpClient()),
            NullLogger<InvoiceItemInterpreter>.Instance);
    }

    [SkippableFact]
    public async Task Modelo_real_converte_o_pedido_em_itens_do_catalogo()
    {
        Skip.If(string.IsNullOrWhiteSpace(Chave), "Ai__ApiKey não configurada");

        var itens = await Interpretador().Interpret(
            "3 parafusos sextavados e dois martelos de borracha", Catalogo, default);

        itens.Count.ShouldBe(2);
        itens.ShouldContain(item => item.Code == "PAR-M8" && item.Quantity == 3);
        itens.ShouldContain(item => item.Code == "MAR-BOR" && item.Quantity == 2);
    }

    [SkippableFact]
    public async Task Modelo_real_nao_inventa_produto_fora_do_catalogo()
    {
        Skip.If(string.IsNullOrWhiteSpace(Chave), "Ai__ApiKey não configurada");

        var itens = await Interpretador().Interpret(
            "5 furadeiras e 2 martelos de borracha", Catalogo, default);

        itens.ShouldAllBe(item => Catalogo.Any(entrada => entrada.Code == item.Code));
    }

    [SkippableFact]
    public async Task Modelo_real_assume_quantidade_um_quando_nao_ha_numero()
    {
        Skip.If(string.IsNullOrWhiteSpace(Chave), "Ai__ApiKey não configurada");

        var itens = await Interpretador().Interpret("uma fita isolante", Catalogo, default);

        itens.Single().Code.ShouldBe("FIT-ISO");
        itens.Single().Quantity.ShouldBe(1);
    }
}
