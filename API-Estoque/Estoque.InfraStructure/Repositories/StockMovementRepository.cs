using Estoque.Domain.Entities;
using Estoque.Domain.Interfaces;
using Estoque.InfraStructure.Data;

namespace Estoque.InfraStructure.Repositories;

public sealed class StockMovementRepository(EstoqueDbContext context) : IStockMovementRepository
{
    public async Task AddRange(IReadOnlyList<StockMovement> movements, CancellationToken ct)
        => await context.StockMovements.AddRangeAsync(movements, ct);
}
