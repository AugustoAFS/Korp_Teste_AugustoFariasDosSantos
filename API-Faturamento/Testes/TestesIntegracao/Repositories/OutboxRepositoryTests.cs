using Faturamento.InfraStructure.Repositories;
using Faturamento.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Faturamento.TestesIntegracao.Repositories;

[Collection(AmbienteCollection.Nome)]
public sealed class OutboxRepositoryTests(PostgresFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> Enfileirar(string tipo = "BaixarEstoqueCommand", string payload = "{}")
    {
        await using var contexto = banco.CreateContext();
        await new OutboxRepository(contexto).Add(tipo, payload, default);
        await contexto.SaveChangesAsync();

        return await contexto.OutboxMessages.AsNoTracking()
            .Where(m => m.Type == tipo)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => m.Id)
            .FirstAsync();
    }

    [Fact]
    public async Task Mensagem_adicionada_aparece_como_pendente()
    {
        await Enfileirar();

        await using var contexto = banco.CreateContext();
        var pendentes = await new OutboxRepository(contexto).GetPending(50, default);

        pendentes.Count.ShouldBe(1);
        pendentes[0].Type.ShouldBe("BaixarEstoqueCommand");
    }

    [Fact]
    public async Task GetPending_respeita_o_limite_do_lote()
    {
        for (var i = 0; i < 5; i++) await Enfileirar(payload: $$"""{"i":{{i}}}""");

        await using var contexto = banco.CreateContext();
        var pendentes = await new OutboxRepository(contexto).GetPending(3, default);

        pendentes.Count.ShouldBe(3);
    }

    [Fact]
    public async Task MarkPublished_tira_a_mensagem_da_fila_de_pendentes()
    {
        var id = await Enfileirar();

        await using var contexto = banco.CreateContext();
        var repositorio = new OutboxRepository(contexto);
        await repositorio.MarkPublished(id, default);

        (await repositorio.GetPending(50, default)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RegisterFailure_incrementa_a_tentativa_e_guarda_o_erro()
    {
        var id = await Enfileirar();

        await using var contexto = banco.CreateContext();
        await new OutboxRepository(contexto).RegisterFailure(id, "broker indisponível", default);

        var mensagem = await contexto.OutboxMessages.AsNoTracking().FirstAsync(m => m.Id == id);
        mensagem.Attempts.ShouldBe(1);
        mensagem.LastError.ShouldBe("broker indisponível");
    }

    [Fact]
    public async Task Mensagem_que_falhou_continua_pendente_ate_o_limite()
    {
        var id = await Enfileirar();

        await using var contexto = banco.CreateContext();
        var repositorio = new OutboxRepository(contexto);
        await repositorio.RegisterFailure(id, "falha", default);

        (await repositorio.GetPending(50, default)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Mensagem_que_esgotou_as_tentativas_sai_da_fila()
    {
        var id = await Enfileirar();

        await using var contexto = banco.CreateContext();
        var repositorio = new OutboxRepository(contexto);

        for (var i = 0; i < OutboxRepository.MaxAttempts; i++)
            await repositorio.RegisterFailure(id, "falha", default);

        (await repositorio.GetPending(50, default)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Pendentes_saem_em_ordem_de_criacao()
    {
        await Enfileirar("BaixarEstoqueCommand");
        await Task.Delay(10);
        await Enfileirar("EstoqueBaixadoEvent");

        await using var contexto = banco.CreateContext();
        var pendentes = await new OutboxRepository(contexto).GetPending(50, default);

        pendentes[0].Type.ShouldBe("BaixarEstoqueCommand");
        pendentes[1].Type.ShouldBe("EstoqueBaixadoEvent");
    }

    [Fact]
    public async Task Payload_volta_desserializavel_para_o_dispatcher_republicar()
    {
        const string payload = """{"NotaFiscalId":42,"ProcessamentoId":"7f9c"}""";
        await Enfileirar(payload: payload);

        await using var contexto = banco.CreateContext();
        var pendentes = await new OutboxRepository(contexto).GetPending(1, default);

        var lido = System.Text.Json.JsonDocument.Parse(pendentes[0].Payload).RootElement;

        lido.GetProperty("NotaFiscalId").GetInt64().ShouldBe(42);
        lido.GetProperty("ProcessamentoId").GetString().ShouldBe("7f9c");
    }

    [Fact]
    public async Task Coluna_jsonb_normaliza_o_texto_mas_preserva_o_conteudo()
    {
        await Enfileirar(payload: """{"b":2,"a":1}""");

        await using var contexto = banco.CreateContext();
        var pendentes = await new OutboxRepository(contexto).GetPending(1, default);

        var lido = System.Text.Json.JsonDocument.Parse(pendentes[0].Payload).RootElement;

        lido.GetProperty("a").GetInt32().ShouldBe(1);
        lido.GetProperty("b").GetInt32().ShouldBe(2);
    }
}
