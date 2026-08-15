namespace Faturamento.Domain.Interfaces;

public interface IUnitOfWork
{
    Task Begin(CancellationToken ct);

    Task<bool> SaveWithoutConflict(CancellationToken ct);

    Task Commit(CancellationToken ct);

    Task Rollback(CancellationToken ct);
}
