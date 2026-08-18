using System.Text.Json;
using Faturamento.Ai.Abstractions;
using Faturamento.Domain.Dtos.Ai;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faturamento.Ai.Features;

public sealed class InvoiceItemInterpreter(
    IChatModel model,
    ILogger<InvoiceItemInterpreter> logger) : IInvoiceItemInterpreter
{
    private const string Instrucao =
        """
        Você converte um pedido em português para itens de nota fiscal.

        Receberá um catálogo de produtos e uma frase do usuário. Devolva os itens
        pedidos usando SOMENTE códigos que aparecem no catálogo.

        Regras:
        - Nunca invente um código que não esteja no catálogo.
        - Se a frase pedir algo que não existe no catálogo, omita o item.
        - Quantidade sem número explícito é 1.
        - O mesmo código não pode aparecer duas vezes; some as quantidades.

        Responda apenas com JSON no formato:
        {"itens":[{"codigo":"...","quantidade":1}]}
        """;

    private static readonly Dictionary<string, JsonElement> Schema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            itens = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        codigo = new { type = "string" },
                        quantidade = new { type = "integer", minimum = 1 }
                    },
                    required = new[] { "codigo", "quantidade" }
                }
            }
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "itens" })
    };

    public bool Enabled => model.Enabled;

    public async Task<IReadOnlyList<ParsedItem>> Interpret(
        string phrase, IReadOnlyList<CatalogEntry> catalog, CancellationToken ct)
    {
        if (!Enabled || catalog.Count == 0) return [];

        var texto = await model.Complete(
            new ChatRequest
            {
                Instruction = Instrucao,
                Prompt = Pergunta(phrase, catalog),
                JsonSchema = Schema
            },
            ct);

        if (string.IsNullOrWhiteSpace(texto))
        {
            logger.LogWarning("Interpretação devolveu resposta vazia");
            return [];
        }

        return Ler(texto);
    }

    private static string Pergunta(string phrase, IReadOnlyList<CatalogEntry> catalog)
        => $"""
            Catálogo disponível:
            {string.Join('\n', catalog.Select(item => $"{item.Code} — {item.Description}"))}

            Pedido do usuário:
            {phrase}
            """;

    private IReadOnlyList<ParsedItem> Ler(string texto)
    {
        try
        {
            using var documento = JsonDocument.Parse(Extrair(texto));

            if (!documento.RootElement.TryGetProperty("itens", out var itens)) return [];

            return
            [
                .. itens.EnumerateArray()
                    .Select(item => new ParsedItem
                    {
                        Code = item.TryGetProperty("codigo", out var codigo) ? codigo.GetString() ?? string.Empty : string.Empty,
                        Quantity = item.TryGetProperty("quantidade", out var qtd) && qtd.TryGetInt32(out var valor) ? valor : 0
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Code) && item.Quantity > 0)
            ];
        }
        catch (JsonException excecao)
        {
            logger.LogWarning(excecao, "Interpretação devolveu JSON inválido");
            return [];
        }
    }

    private static string Extrair(string texto)
    {
        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');

        return inicio >= 0 && fim > inicio ? texto[inicio..(fim + 1)] : texto;
    }
}
