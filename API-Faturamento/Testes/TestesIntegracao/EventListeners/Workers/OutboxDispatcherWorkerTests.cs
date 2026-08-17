using System.Reflection;
using Faturamento.EventListeners.Messages.Publicados;
using Faturamento.EventListeners.Workers;
using Faturamento.TestesIntegracao.Suporte;
using MassTransit;
using Shouldly;

namespace Faturamento.TestesIntegracao.EventListeners.Workers;

public sealed class OutboxDispatcherWorkerTests
{
    private static IReadOnlyDictionary<string, Type> MapaDeTipos()
        => (IReadOnlyDictionary<string, Type>)typeof(OutboxDispatcherWorker)
            .GetField("Tipos", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    private static Type[] MensagensPublicaveis()
        => [.. typeof(BaixarEstoqueCommand).Assembly
            .GetTypes()
            .Where(tipo => tipo.Namespace == typeof(BaixarEstoqueCommand).Namespace)
            .Where(tipo => tipo.GetCustomAttribute<MessageUrnAttribute>() is not null)];

    [Fact]
    public void Toda_mensagem_publicavel_esta_no_mapa_do_dispatcher()
    {
        var mapa = MapaDeTipos();

        foreach (var mensagem in MensagensPublicaveis())
            mapa.Values.ShouldContain(
                mensagem,
                $"{mensagem.Name} não está no mapa do OutboxDispatcherWorker e ficaria preso no outbox.");
    }

    [Fact]
    public void Chave_do_mapa_e_o_nome_do_tipo_gravado_no_outbox()
    {
        foreach (var (nome, tipo) in MapaDeTipos())
            nome.ShouldBe(tipo.Name);
    }

    [Fact]
    public void Mapa_nao_aponta_para_tipo_sem_contrato_de_mensagem()
    {
        foreach (var tipo in MapaDeTipos().Values)
            tipo.GetCustomAttribute<MessageUrnAttribute>().ShouldNotBeNull();
    }

    [Fact]
    public void Comando_de_baixa_e_o_que_o_faturamento_publica()
        => MapaDeTipos().Values.ShouldContain(typeof(BaixarEstoqueCommand));
}
