using Estoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoque.InfraStructure.Data.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(m => m.Id).IsClustered(false);

        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.ProductId).IsRequired();
        builder.Property(m => m.Type).HasConversion<byte>().IsRequired();
        builder.Property(m => m.Quantity).IsRequired();
        builder.Property(m => m.BalanceBefore).IsRequired();
        builder.Property(m => m.BalanceAfter).IsRequired();
        builder.Property(m => m.OccurredAt).HasColumnType("datetimeoffset(3)").IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.OccurredAt)
            .IsClustered()
            .HasDatabaseName("ix_stock_movements_occurred_at");

        builder.HasIndex(m => new { m.IdempotencyKey, m.ProductId })
            .IsUnique()
            .HasFilter("[idempotency_key] IS NOT NULL")
            .HasDatabaseName("ux_stock_movements_idempotency");

        builder.HasIndex(m => new { m.ProductId, m.OccurredAt })
            .HasDatabaseName("ix_stock_movements_product");
    }
}
