using Estoque.Domain.Entities;

namespace Estoque.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task AddRange(IReadOnlyList<StockMovement> movements, CancellationToken ct);
}
