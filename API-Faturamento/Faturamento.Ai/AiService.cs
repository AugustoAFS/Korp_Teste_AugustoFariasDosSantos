using Faturamento.Ai.Abstractions;
using Faturamento.Ai.Features;
using Faturamento.Ai.Providers;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Faturamento.Ai;

public static class AiService
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        var opcoes = Ler(configuration.GetSection(AiOptions.Section));

        services.AddSingleton(opcoes);
        services.AddHttpClient();
        services.AddSingleton<IChatModel>(provedor => Modelo(opcoes, provedor));
        services.AddSingleton<IInvoiceItemInterpreter, InvoiceItemInterpreter>();
        services.AddSingleton<IRejectionExplainer, RejectionExplainer>();

        return services;
    }

    public static void WarnWhenAiDisabled(this IServiceProvider services)
    {
        var opcoes = services.GetRequiredService<AiOptions>();

        if (opcoes.Enabled) return;

        services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(AiService))
            .LogWarning(
                "IA desabilitada: {Chave} não configurada. Os demais recursos funcionam normalmente.",
                $"{AiOptions.Section}:ApiKey");
    }

    private static IChatModel Modelo(AiOptions opcoes, IServiceProvider provedor)
        => opcoes.Enabled
            ? new ChatCompletionsModel(opcoes, Http(provedor))
            : new DisabledChatModel();

    private static HttpClient Http(IServiceProvider provedor)
        => provedor.GetRequiredService<IHttpClientFactory>().CreateClient("ai");

    private static AiOptions Ler(IConfigurationSection secao)
    {
        if (!secao.Exists())
            throw new InvalidOperationException($"Seção {AiOptions.Section} não configurada.");

        return new AiOptions
        {
            ApiKey = secao["ApiKey"] ?? string.Empty,
            Model = Obrigatorio(secao["Model"], "Model"),
            MaxTokens = Numero(secao["MaxTokens"], "MaxTokens"),
            TimeoutSeconds = Numero(secao["TimeoutSeconds"], "TimeoutSeconds"),
            BaseUrl = Obrigatorio(secao["BaseUrl"], "BaseUrl")
        };
    }

    private static string Obrigatorio(string? valor, string chave)
        => string.IsNullOrWhiteSpace(valor)
            ? throw new InvalidOperationException($"{AiOptions.Section}:{chave} não configurada.")
            : valor;

    private static int Numero(string? valor, string chave)
        => int.TryParse(valor, out var numero) && numero > 0
            ? numero
            : throw new InvalidOperationException($"{AiOptions.Section}:{chave} deve ser um inteiro positivo.");
}
