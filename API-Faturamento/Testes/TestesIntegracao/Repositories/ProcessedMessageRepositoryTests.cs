using Faturamento.InfraStructure.Repositories;
using Faturamento.TestesIntegracao.Suporte;
using Shouldly;

namespace Faturamento.TestesIntegracao.Repositories;

[Collection(AmbienteCollection.Nome)]
public sealed class ProcessedMessageRepositoryTests(PostgresFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Mensagem_inedita_ainda_nao_foi_processada()
    {
        await using var contexto = banco.CreateContext();

        (await new ProcessedMessageRepository(contexto).AlreadyProcessed(Guid.CreateVersion7(), default))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Mark_persistido_faz_a_mensagem_contar_como_processada()
    {
        var chave = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
        {
            await new ProcessedMessageRepository(contexto).Mark(chave, "EstoqueBaixadoEvent", default);
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
            await new ProcessedMessageRepository(contexto).Mark(chave, "EstoqueBaixadoEvent", default);
            await contexto.SaveChangesAsync();
        }

        await using var segunda = banco.CreateContext();
        await new ProcessedMessageRepository(segunda).Mark(chave, "EstoqueBaixadoEvent", default);

        await Should.ThrowAsync<Exception>(() => segunda.SaveChangesAsync());
    }

    [Fact]
    public async Task Marcador_de_outra_mensagem_nao_bloqueia_a_minha()
    {
        await using var contexto = banco.CreateContext();
        var repositorio = new ProcessedMessageRepository(contexto);

        await repositorio.Mark(Guid.CreateVersion7(), "EstoqueBaixadoEvent", default);
        await contexto.SaveChangesAsync();

        (await repositorio.AlreadyProcessed(Guid.CreateVersion7(), default)).ShouldBeFalse();
    }
}
