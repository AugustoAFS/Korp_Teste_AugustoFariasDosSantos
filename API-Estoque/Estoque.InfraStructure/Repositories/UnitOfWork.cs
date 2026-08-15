using Estoque.Domain.Interfaces;
using Estoque.InfraStructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Estoque.InfraStructure.Repositories;

public sealed class UnitOfWork(EstoqueDbContext context) : IUnitOfWork
{
    private const int IndiceUnicoViolado = 2601;
    private const int ChaveDuplicada = 2627;

    public async Task Begin(CancellationToken ct)
        => await context.Database.BeginTransactionAsync(ct);

    public async Task<bool> SaveWithoutConflict(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: IndiceUnicoViolado or ChaveDuplicada })
        {
            return false;
        }
    }

    public Task CreateSavepoint(string nome, CancellationToken ct)
        => Current().CreateSavepointAsync(nome, ct);

    public Task RollbackToSavepoint(string nome, CancellationToken ct)
        => Current().RollbackToSavepointAsync(nome, ct);

    public async Task Commit(CancellationToken ct)
        => await context.Database.CommitTransactionAsync(ct);

    public async Task Rollback(CancellationToken ct)
    {
        if (context.Database.CurrentTransaction is null) return;

        await context.Database.RollbackTransactionAsync(ct);
    }

    private IDbContextTransaction Current()
        => context.Database.CurrentTransaction
           ?? throw new InvalidOperationException("Nenhuma transação em andamento.");
}
