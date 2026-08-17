using System.Reflection;
using Faturamento.EventListeners.Workers;
using Shouldly;

namespace Faturamento.TestesIntegracao.EventListeners.Workers;

public sealed class PrintExpirationWorkerTests
{
    private static T Constante<T>(string nome)
    {
        var campo = typeof(PrintExpirationWorker)
            .GetField(nome, BindingFlags.NonPublic | BindingFlags.Static)!;

        return (T)campo.GetValue(null)!;
    }

    [Fact]
    public void Tolerancia_e_maior_que_o_intervalo_de_varredura()
    {
        var tolerancia = Constante<TimeSpan>("Tolerancia");
        var intervalo = Constante<TimeSpan>("Intervalo");

        tolerancia.ShouldBeGreaterThan(intervalo);
    }

    [Fact]
    public void Varredura_roda_com_folga_para_o_estoque_responder()
        => Constante<TimeSpan>("Tolerancia").TotalSeconds.ShouldBeGreaterThanOrEqualTo(60);

    [Fact]
    public void Lote_e_limitado_para_nao_varrer_a_tabela_inteira()
        => Constante<int>("Lote").ShouldBeInRange(1, 500);

    [Fact]
    public void Worker_e_um_background_service()
        => typeof(PrintExpirationWorker).BaseType!.Name.ShouldBe("BackgroundService");
}
