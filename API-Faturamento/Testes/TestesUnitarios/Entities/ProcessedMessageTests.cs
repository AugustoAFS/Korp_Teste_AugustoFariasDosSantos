using Faturamento.Domain.Entities;
using Shouldly;

namespace Faturamento.TestesUnitarios.Entities;

public sealed class ProcessedMessageTests
{
    [Fact]
    public void Marcador_guarda_a_chave_e_o_tipo_da_mensagem()
    {
        var chave = Guid.CreateVersion7();

        var marcador = new ProcessedMessage(chave, "EstoqueBaixadoEvent");

        marcador.MessageId.ShouldBe(chave);
        marcador.Type.ShouldBe("EstoqueBaixadoEvent");
    }

    [Fact]
    public void Marcador_registra_o_instante_do_processamento()
    {
        var antes = DateTimeOffset.UtcNow;

        var marcador = new ProcessedMessage(Guid.CreateVersion7(), "EstoqueBaixadoEvent");

        marcador.ProcessedAt.ShouldBeInRange(antes, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Faturamento_nao_guarda_desfecho_porque_nao_reemite_resultado()
        => typeof(ProcessedMessage)
            .GetProperties()
            .Select(propriedade => propriedade.Name)
            .ShouldNotContain("OutcomeType");
}
