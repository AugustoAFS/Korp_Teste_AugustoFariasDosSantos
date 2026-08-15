using Estoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoque.InfraStructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table => table.HasCheckConstraint("ck_products_balance", "[balance] >= 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Code).HasColumnType("varchar(50)").IsRequired();
        builder.Property(p => p.Description).HasColumnType("varchar(200)").IsRequired();
        builder.Property(p => p.Balance).IsRequired();
        builder.Property(p => p.Active).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnType("datetimeoffset(3)");
        builder.Property(p => p.DeletedAt).HasColumnType("datetimeoffset(3)");

        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasFilter("[deleted_at] IS NULL")
            .HasDatabaseName("ux_products_code");

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
