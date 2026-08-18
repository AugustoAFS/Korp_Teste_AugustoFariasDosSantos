using Faturamento.Ai;
using Faturamento.Ai.Abstractions;
using Faturamento.Ai.Providers;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Faturamento.TestesUnitarios.Ai;

public sealed class AiServiceTests
{
    private static IServiceProvider Montar(Dictionary<string, string?> configuracao)
    {
        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddAi(new ConfigurationBuilder().AddInMemoryCollection(configuracao).Build());

        return servicos.BuildServiceProvider();
    }

    private static Dictionary<string, string?> Valida(string chave = "sk-teste")
        => new()
        {
            ["Ai:BaseUrl"] = "https://api.anthropic.com/v1",
            ["Ai:ApiKey"] = chave,
            ["Ai:Model"] = "claude-haiku-4-5",
            ["Ai:MaxTokens"] = "1024",
            ["Ai:TimeoutSeconds"] = "15"
        };

    #region Escolha do fornecedor

    [Theory]
    [InlineData("https://api.anthropic.com/v1", "claude-haiku-4-5")]
    [InlineData("https://generativelanguage.googleapis.com/v1beta/openai", "gemini-2.0-flash")]
    [InlineData("https://api.openai.com/v1", "gpt-4o-mini")]
    [InlineData("http://localhost:11434/v1", "llama3.2")]
    public void Trocar_o_fornecedor_e_so_trocar_a_base_url(string baseUrl, string modelo)
    {
        var configuracao = Valida();
        configuracao["Ai:BaseUrl"] = baseUrl;
        configuracao["Ai:Model"] = modelo;

        var servicos = Montar(configuracao);

        servicos.GetRequiredService<IChatModel>().ShouldBeOfType<ChatCompletionsModel>();
        servicos.GetRequiredService<AiOptions>().Model.ShouldBe(modelo);
    }

    [Fact]
    public void Sem_chave_o_modelo_e_o_desligado_seja_qual_for_a_base_url()
        => Montar(Valida(chave: string.Empty)).GetRequiredService<IChatModel>()
            .ShouldBeOfType<DisabledChatModel>();

    #endregion

    #region As features não conhecem fornecedor

    [Fact]
    public void Interpretador_e_registrado_pela_interface_do_dominio()
        => Montar(Valida()).GetRequiredService<IInvoiceItemInterpreter>().ShouldNotBeNull();

    [Fact]
    public void Explicador_e_registrado_pela_interface_do_dominio()
        => Montar(Valida()).GetRequiredService<IRejectionExplainer>().ShouldNotBeNull();

    [Fact]
    public void Sem_chave_as_features_existem_mas_se_declaram_desligadas()
    {
        var servicos = Montar(Valida(chave: string.Empty));

        servicos.GetRequiredService<IInvoiceItemInterpreter>().Enabled.ShouldBeFalse();
        servicos.GetRequiredService<IRejectionExplainer>().Enabled.ShouldBeFalse();
    }

    #endregion

    #region Configuração obrigatória

    [Fact]
    public void Secao_ausente_derruba_o_boot()
        => Should.Throw<InvalidOperationException>(() => Montar([]))
            .Message.ShouldContain("Ai");

    [Theory]
    [InlineData("Ai:BaseUrl")]
    [InlineData("Ai:Model")]
    public void Chave_obrigatoria_ausente_derruba_o_boot_nomeando_a_chave(string chave)
    {
        var configuracao = Valida();
        configuracao[chave] = null;

        Should.Throw<InvalidOperationException>(() => Montar(configuracao))
            .Message.ShouldContain(chave.Split(':')[1]);
    }

    [Theory]
    [InlineData("Ai:MaxTokens", "zero")]
    [InlineData("Ai:MaxTokens", "0")]
    [InlineData("Ai:TimeoutSeconds", "-5")]
    public void Numero_invalido_derruba_o_boot(string chave, string valor)
    {
        var configuracao = Valida();
        configuracao[chave] = valor;

        Should.Throw<InvalidOperationException>(() => Montar(configuracao))
            .Message.ShouldContain("inteiro positivo");
    }

    #endregion

    #region Aviso de degradação

    [Fact]
    public void Boot_sem_chave_avisa_nomeando_Ai_ApiKey()
    {
        var servicos = Montar(Valida(chave: string.Empty));

        Should.NotThrow(() => servicos.WarnWhenAiDisabled());
    }

    [Fact]
    public void Boot_com_chave_nao_avisa()
    {
        var servicos = Montar(Valida());

        Should.NotThrow(() => servicos.WarnWhenAiDisabled());
    }

    #endregion
}
