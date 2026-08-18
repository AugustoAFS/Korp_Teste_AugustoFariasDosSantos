using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Faturamento.Ai.Abstractions;

namespace Faturamento.Ai.Providers;

public sealed class ChatCompletionsModel(AiOptions options, HttpClient client) : IChatModel
{
    private const int Tentativas = 3;

    private static readonly TimeSpan Espera = TimeSpan.FromMilliseconds(600);

    private static readonly HashSet<HttpStatusCode> Transitorios =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    public bool Enabled => options.Enabled;

    public async Task<string?> Complete(ChatRequest request, CancellationToken ct)
    {
        if (!Enabled) return null;

        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(ct);
        prazo.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        for (var tentativa = 1; ; tentativa++)
        {
            using var resposta = await Enviar(request, prazo.Token);

            if (resposta.IsSuccessStatusCode)
                return Texto(await resposta.Content.ReadFromJsonAsync<JsonElement>(prazo.Token));

            if (tentativa == Tentativas || !Transitorios.Contains(resposta.StatusCode))
                resposta.EnsureSuccessStatusCode();

            await Task.Delay(Espera * tentativa, prazo.Token);
        }
    }

    private Task<HttpResponseMessage> Enviar(ChatRequest request, CancellationToken ct)
    {
        var requisicao = new HttpRequestMessage(
            HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(Corpo(request))
        };

        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        return client.SendAsync(requisicao, ct);
    }

    private object Corpo(ChatRequest request)
        => new
        {
            model = options.Model,
            max_tokens = options.MaxTokens,
            messages = new[]
            {
                new { role = "system", content = request.Instruction },
                new { role = "user", content = request.Prompt }
            },
            response_format = request.JsonSchema is null ? null : new { type = "json_object" }
        };

    private static string? Texto(JsonElement resposta)
    {
        if (!resposta.TryGetProperty("choices", out var escolhas)) return null;

        foreach (var escolha in escolhas.EnumerateArray())
        {
            if (!escolha.TryGetProperty("message", out var mensagem)) continue;
            if (!mensagem.TryGetProperty("content", out var conteudo)) continue;

            var valor = conteudo.GetString()?.Trim();

            if (!string.IsNullOrWhiteSpace(valor)) return valor;
        }

        return null;
    }
}
