using Faturamento.Domain.Interfaces;
using Faturamento.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Faturamento.InfraStructure.Repositories;

public sealed class UnitOfWork(FaturamentoDbContext context) : IUnitOfWork
{
    private const string ViolacaoDeUnicidade = "23505";

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
            when (ex.InnerException is PostgresException { SqlState: ViolacaoDeUnicidade })
        {
            return false;
        }
    }

    public async Task Commit(CancellationToken ct)
        => await context.Database.CommitTransactionAsync(ct);

    public async Task Rollback(CancellationToken ct)
    {
        if (context.Database.CurrentTransaction is null) return;

        await context.Database.RollbackTransactionAsync(ct);
    }
}
