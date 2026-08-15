namespace Estoque.Domain.Interfaces;

public interface IUnitOfWork
{
    Task Begin(CancellationToken ct);

    Task<bool> SaveWithoutConflict(CancellationToken ct);

    Task CreateSavepoint(string nome, CancellationToken ct);

    Task RollbackToSavepoint(string nome, CancellationToken ct);

    Task Commit(CancellationToken ct);

    Task Rollback(CancellationToken ct);
}
