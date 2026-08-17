using Faturamento.Domain.Entities;
using Shouldly;

namespace Faturamento.TestesUnitarios.Entities;

public sealed class OutboxMessageTests
{
    [Fact]
    public void Mensagem_nasce_pendente_de_publicacao()
    {
        var mensagem = new OutboxMessage("BaixarEstoqueCommand", """{"NotaFiscalId":42}""");

        mensagem.PublishedAt.ShouldBeNull();
        mensagem.Attempts.ShouldBe(0);
        mensagem.LastError.ShouldBeNull();
    }

    [Fact]
    public void Mensagem_guarda_o_tipo_e_o_payload_que_o_dispatcher_usa_para_publicar()
    {
        var mensagem = new OutboxMessage("BaixarEstoqueCommand", """{"NotaFiscalId":42}""");

        mensagem.Type.ShouldBe("BaixarEstoqueCommand");
        mensagem.Payload.ShouldBe("""{"NotaFiscalId":42}""");
    }

    [Fact]
    public void Mensagem_registra_o_instante_de_criacao_e_um_identificador_proprio()
    {
        var antes = DateTimeOffset.UtcNow;

        var mensagem = new OutboxMessage("BaixarEstoqueCommand", "{}");

        mensagem.Id.ShouldNotBe(Guid.Empty);
        mensagem.CreatedAt.ShouldBeInRange(antes, DateTimeOffset.UtcNow);
    }
}
