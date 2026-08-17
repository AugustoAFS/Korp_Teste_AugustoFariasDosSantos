using Estoque.Domain.Entities;
using Estoque.InfraStructure.Repositories;
using Estoque.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Estoque.TestesIntegracao.Repositories;

[Collection(BancoCollection.Nome)]
public sealed class UnitOfWorkTests(SqlServerFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<int> ProdutosGravados()
    {
        await using var contexto = banco.CreateContext();
        return await contexto.Products.AsNoTracking().CountAsync();
    }

    [Fact]
    public async Task Commit_persiste_o_que_foi_gravado_na_transacao()
    {
        await using (var contexto = banco.CreateContext())
        {
            var unitOfWork = new UnitOfWork(contexto);
            await unitOfWork.Begin(default);
            contexto.Products.Add(new Product("PAR-M8", "Parafuso", 1));
            await unitOfWork.SaveWithoutConflict(default);
            await unitOfWork.Commit(default);
        }

        (await ProdutosGravados()).ShouldBe(1);
    }

    [Fact]
    public async Task Rollback_descarta_tudo_que_foi_gravado_na_transacao()
    {
        await using (var contexto = banco.CreateContext())
        {
            var unitOfWork = new UnitOfWork(contexto);
            await unitOfWork.Begin(default);
            contexto.Products.Add(new Product("PAR-M8", "Parafuso", 1));
            await unitOfWork.SaveWithoutConflict(default);
            await unitOfWork.Rollback(default);
        }

        (await ProdutosGravados()).ShouldBe(0);
    }

    [Fact]
    public async Task Rollback_sem_transacao_aberta_nao_explode()
    {
        await using var contexto = banco.CreateContext();

        await Should.NotThrowAsync(() => new UnitOfWork(contexto).Rollback(default));
    }

    [Fact]
    public async Task SaveWithoutConflict_devolve_false_quando_o_codigo_ja_existe()
    {
        await using (var semente = banco.CreateContext())
        {
            semente.Products.Add(new Product("PAR-M8", "Parafuso", 1));
            await semente.SaveChangesAsync();
        }

        await using var contexto = banco.CreateContext();
        var unitOfWork = new UnitOfWork(contexto);
        await unitOfWork.Begin(default);
        contexto.Products.Add(new Product("PAR-M8", "Duplicado", 1));

        var salvou = await unitOfWork.SaveWithoutConflict(default);

        salvou.ShouldBeFalse();
        await unitOfWork.Rollback(default);
    }

    [Fact]
    public async Task SaveWithoutConflict_devolve_true_no_caminho_normal()
    {
        await using var contexto = banco.CreateContext();
        var unitOfWork = new UnitOfWork(contexto);
        await unitOfWork.Begin(default);
        contexto.Products.Add(new Product("PAR-M8", "Parafuso", 1));

        (await unitOfWork.SaveWithoutConflict(default)).ShouldBeTrue();

        await unitOfWork.Commit(default);
    }

    #region Savepoint

    [Fact]
    public async Task Savepoint_desfaz_apenas_o_que_veio_depois_dele()
    {
        await using (var contexto = banco.CreateContext())
        {
            var unitOfWork = new UnitOfWork(contexto);
            await unitOfWork.Begin(default);

            contexto.Products.Add(new Product("ANTES", "Gravado antes do savepoint", 1));
            await unitOfWork.SaveWithoutConflict(default);

            await unitOfWork.CreateSavepoint("marco", default);

            contexto.Products.Add(new Product("DEPOIS", "Gravado depois do savepoint", 1));
            await unitOfWork.SaveWithoutConflict(default);

            await unitOfWork.RollbackToSavepoint("marco", default);
            await unitOfWork.Commit(default);
        }

        await using var conferencia = banco.CreateContext();
        var codigos = await conferencia.Products.AsNoTracking().Select(p => p.Code).ToListAsync();

        codigos.ShouldContain("ANTES");
        codigos.ShouldNotContain("DEPOIS");
    }

    [Fact]
    public async Task Savepoint_sem_transacao_aberta_explica_o_erro()
    {
        await using var contexto = banco.CreateContext();

        var excecao = await Should.ThrowAsync<InvalidOperationException>(
            () => new UnitOfWork(contexto).CreateSavepoint("marco", default));

        excecao.Message.ShouldContain("transação");
    }

    #endregion
}
