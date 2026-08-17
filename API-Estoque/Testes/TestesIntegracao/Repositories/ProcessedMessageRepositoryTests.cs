using Estoque.Domain.Dtos.EventListeners;
using Estoque.InfraStructure.Repositories;
using Estoque.TestesIntegracao.Suporte;
using Shouldly;

namespace Estoque.TestesIntegracao.Repositories;

[Collection(BancoCollection.Nome)]
public sealed class ProcessedMessageRepositoryTests(SqlServerFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Mensagem_inedita_ainda_nao_foi_processada()
    {
        await using var contexto = banco.CreateContext();

        var jaProcessada = await new ProcessedMessageRepository(contexto)
            .AlreadyProcessed(Guid.CreateVersion7(), default);

        jaProcessada.ShouldBeFalse();
    }

    [Fact]
    public async Task Mark_persistido_faz_a_mensagem_contar_como_processada()
    {
        var chave = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
        {
            await new ProcessedMessageRepository(contexto).Mark(chave, "BaixarEstoqueCommand", default);
            await contexto.SaveChangesAsync();
        }

        await using var conferencia = banco.CreateContext();
        (await new ProcessedMessageRepository(conferencia).AlreadyProcessed(chave, default)).ShouldBeTrue();
    }

    [Fact]
    public async Task Chave_duplicada_e_recusada_pelo_banco()
    {
        var chave = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
        {
            await new ProcessedMessageRepository(contexto).Mark(chave, "BaixarEstoqueCommand", default);
            await contexto.SaveChangesAsync();
        }

        await using var segunda = banco.CreateContext();
        await new ProcessedMessageRepository(segunda).Mark(chave, "BaixarEstoqueCommand", default);

        await Should.ThrowAsync<Exception>(() => segunda.SaveChangesAsync());
    }

    #region Replay do desfecho — requisito opcional (c)

    [Fact]
    public async Task Marcador_sem_desfecho_gravado_nao_devolve_evento()
    {
        var chave = Guid.CreateVersion7();

        await using var contexto = banco.CreateContext();
        var repositorio = new ProcessedMessageRepository(contexto);
        await repositorio.Mark(chave, "BaixarEstoqueCommand", default);
        await contexto.SaveChangesAsync();

        (await repositorio.Outcome(chave, default)).ShouldBeNull();
    }

    [Fact]
    public async Task Desfecho_gravado_volta_intacto_para_ser_reemitido()
    {
        var chave = Guid.CreateVersion7();
        var evento = new StoredEvent
        {
            Type = "EstoqueBaixadoEvent",
            Payload = """{"NotaFiscalId":42,"ProcessamentoId":"abc"}"""
        };

        await using var contexto = banco.CreateContext();
        var repositorio = new ProcessedMessageRepository(contexto);
        await repositorio.Mark(chave, "BaixarEstoqueCommand", default);
        await contexto.SaveChangesAsync();
        await repositorio.RecordOutcome(chave, evento, default);

        var guardado = await repositorio.Outcome(chave, default);

        guardado.ShouldNotBeNull();
        guardado.Type.ShouldBe(evento.Type);
        guardado.Payload.ShouldBe(evento.Payload);
    }

    [Fact]
    public async Task Desfecho_de_rejeicao_tambem_e_guardado_para_replay()
    {
        var chave = Guid.CreateVersion7();

        await using var contexto = banco.CreateContext();
        var repositorio = new ProcessedMessageRepository(contexto);
        await repositorio.Mark(chave, "BaixarEstoqueCommand", default);
        await contexto.SaveChangesAsync();
        await repositorio.RecordOutcome(
            chave, new StoredEvent { Type = "EstoqueRejeitadoEvent", Payload = "{}" }, default);

        var guardado = await repositorio.Outcome(chave, default);

        guardado!.Type.ShouldBe("EstoqueRejeitadoEvent");
    }

    [Fact]
    public async Task Desfecho_de_outra_mensagem_nao_vaza()
    {
        var minha = Guid.CreateVersion7();
        var outra = Guid.CreateVersion7();

        await using var contexto = banco.CreateContext();
        var repositorio = new ProcessedMessageRepository(contexto);
        await repositorio.Mark(outra, "BaixarEstoqueCommand", default);
        await contexto.SaveChangesAsync();
        await repositorio.RecordOutcome(
            outra, new StoredEvent { Type = "EstoqueBaixadoEvent", Payload = "{}" }, default);

        (await repositorio.Outcome(minha, default)).ShouldBeNull();
    }

    #endregion
}
