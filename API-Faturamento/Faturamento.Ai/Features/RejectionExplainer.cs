using Faturamento.Ai.Abstractions;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faturamento.Ai.Features;

public sealed class RejectionExplainer(
    IChatModel model,
    ILogger<RejectionExplainer> logger) : IRejectionExplainer
{
    private const string Instrucao =
        """
        Você explica ao usuário por que a impressão de uma nota fiscal foi recusada
        pelo estoque, em português do Brasil.

        Regras:
        - No máximo duas frases curtas.
        - Diga o que aconteceu e o que o usuário pode fazer.
        - Use os nomes dos produtos, não códigos internos.
        - Não invente números que não estejam no motivo técnico.
        - Não peça desculpas nem use jargão de sistema.
        """;

    public bool Enabled => model.Enabled;

    public async Task<string?> Explain(
        string technicalReason, IReadOnlyList<string> invoiceItems, CancellationToken ct)
    {
        if (!Enabled) return null;

        try
        {
            return await model.Complete(
                new ChatRequest
                {
                    Instruction = Instrucao,
                    Prompt = Pergunta(technicalReason, invoiceItems)
                },
                ct);
        }
        catch (Exception excecao)
        {
            logger.LogWarning(excecao, "Explicação da rejeição indisponível; a nota segue com o motivo técnico");
            return null;
        }
    }

    private static string Pergunta(string technicalReason, IReadOnlyList<string> invoiceItems)
        => $"""
            Itens da nota:
            {string.Join('\n', invoiceItems)}

            Motivo técnico da recusa:
            {technicalReason}
            """;
}
