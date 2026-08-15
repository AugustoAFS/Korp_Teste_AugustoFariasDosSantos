using Faturamento.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.InfraStructure.Data;

public sealed class FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : DbContext(options)
{
    public const string InvoiceNumberSequence = "seq_invoice_number";

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<ReplicatedProduct> ReplicatedProducts => Set<ReplicatedProduct>();
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
    {
        modelBuilder.HasSequence<long>(InvoiceNumberSequence);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FaturamentoDbContext).Assembly);
    }
}
