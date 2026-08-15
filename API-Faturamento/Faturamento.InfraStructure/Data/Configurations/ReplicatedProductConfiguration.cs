using Faturamento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faturamento.InfraStructure.Data.Configurations;

public sealed class ReplicatedProductConfiguration : IEntityTypeConfiguration<ReplicatedProduct>
{
    public void Configure(EntityTypeBuilder<ReplicatedProduct> builder)
    {
        builder.ToTable("replicated_products");

        builder.HasKey(p => p.ProductId);

        builder.Property(p => p.ProductId).ValueGeneratedNever();
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Active).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.Code).HasDatabaseName("ix_replicated_products_code");
    }
}
