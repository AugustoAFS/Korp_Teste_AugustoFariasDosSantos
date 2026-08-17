using Faturamento.Domain.Entities;
using Faturamento.InfraStructure.Repositories;
using Faturamento.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Faturamento.TestesIntegracao.Repositories;

[Collection(AmbienteCollection.Nome)]
public sealed class UnitOfWorkTests(PostgresFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<int> NotasGravadas()
    {
        await using var contexto = banco.CreateContext();
        return await contexto.Invoices.AsNoTracking().CountAsync();
    }

    [Fact]
    public async Task Commit_persiste_o_que_foi_gravado_na_transacao()
    {
        await using (var contexto = banco.CreateContext())
        {
            var unitOfWork = new UnitOfWork(contexto);
            await unitOfWork.Begin(default);
            contexto.Invoices.Add(new Invoice(1, 7, "Augusto"));
            await unitOfWork.SaveWithoutConflict(default);
            await unitOfWork.Commit(default);
        }

        (await NotasGravadas()).ShouldBe(1);
    }

    [Fact]
    public async Task Rollback_descarta_tudo_que_foi_gravado_na_transacao()
    {
        await using (var contexto = banco.CreateContext())
        {
            var unitOfWork = new UnitOfWork(contexto);
            await unitOfWork.Begin(default);
            contexto.Invoices.Add(new Invoice(1, 7, "Augusto"));
            await unitOfWork.SaveWithoutConflict(default);
            await unitOfWork.Rollback(default);
        }

        (await NotasGravadas()).ShouldBe(0);
    }

    [Fact]
    public async Task Rollback_sem_transacao_aberta_nao_explode()
    {
        await using var contexto = banco.CreateContext();

        await Should.NotThrowAsync(() => new UnitOfWork(contexto).Rollback(default));
    }

    [Fact]
    public async Task SaveWithoutConflict_devolve_true_no_caminho_normal()
    {
        await using var contexto = banco.CreateContext();
        var unitOfWork = new UnitOfWork(contexto);
        await unitOfWork.Begin(default);
        contexto.Invoices.Add(new Invoice(1, 7, "Augusto"));

        (await unitOfWork.SaveWithoutConflict(default)).ShouldBeTrue();

        await unitOfWork.Commit(default);
    }

    [Fact]
    public async Task SaveWithoutConflict_devolve_false_quando_a_chave_ja_existe()
    {
        var chave = Guid.CreateVersion7();

        await using (var semente = banco.CreateContext())
        {
            semente.ProcessedMessages.Add(new ProcessedMessage(chave, "EstoqueBaixadoEvent"));
            await semente.SaveChangesAsync();
        }

        await using var contexto = banco.CreateContext();
        var unitOfWork = new UnitOfWork(contexto);
        await unitOfWork.Begin(default);
        contexto.ProcessedMessages.Add(new ProcessedMessage(chave, "EstoqueBaixadoEvent"));

        var salvou = await unitOfWork.SaveWithoutConflict(default);

        salvou.ShouldBeFalse();
        await unitOfWork.Rollback(default);
    }
}
