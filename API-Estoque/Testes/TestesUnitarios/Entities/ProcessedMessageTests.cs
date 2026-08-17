using Estoque.Domain.Entities;
using Shouldly;

namespace Estoque.TestesUnitarios.Entities;

public sealed class ProcessedMessageTests
{
    [Fact]
    public void Marcador_nasce_sem_desfecho_registrado()
    {
        var marcador = new ProcessedMessage(Guid.CreateVersion7(), "BaixarEstoqueCommand");

        marcador.OutcomeType.ShouldBeNull();
        marcador.OutcomePayload.ShouldBeNull();
    }

    [Fact]
    public void Marcador_guarda_a_chave_e_o_tipo_da_mensagem()
    {
        var chave = Guid.CreateVersion7();

        var marcador = new ProcessedMessage(chave, "BaixarEstoqueCommand");

        marcador.MessageId.ShouldBe(chave);
        marcador.Type.ShouldBe("BaixarEstoqueCommand");
    }

    [Fact]
    public void RecordOutcome_guarda_o_evento_que_sera_reemitido_numa_duplicata()
    {
        var marcador = new ProcessedMessage(Guid.CreateVersion7(), "BaixarEstoqueCommand");

        marcador.RecordOutcome("EstoqueBaixadoEvent", """{"NotaFiscalId":42}""");

        marcador.OutcomeType.ShouldBe("EstoqueBaixadoEvent");
        marcador.OutcomePayload.ShouldBe("""{"NotaFiscalId":42}""");
    }

    [Fact]
    public void RecordOutcome_sobrescreve_o_desfecho_anterior()
    {
        var marcador = new ProcessedMessage(Guid.CreateVersion7(), "BaixarEstoqueCommand");

        marcador.RecordOutcome("EstoqueRejeitadoEvent", "{}");
        marcador.RecordOutcome("EstoqueBaixadoEvent", """{"NotaFiscalId":7}""");

        marcador.OutcomeType.ShouldBe("EstoqueBaixadoEvent");
        marcador.OutcomePayload.ShouldBe("""{"NotaFiscalId":7}""");
    }
}
