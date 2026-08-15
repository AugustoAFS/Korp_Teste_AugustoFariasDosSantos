using Estoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estoque.InfraStructure.Data;

public sealed class EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        var agora = DateTimeOffset.UtcNow;

        foreach (var entrada in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entrada.State == EntityState.Added)
                entrada.Property(e => e.CreatedAt).CurrentValue = agora;
            else if (entrada.State == EntityState.Modified)
                entrada.Property(e => e.UpdatedAt).CurrentValue = agora;
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(EstoqueDbContext).Assembly);
}
