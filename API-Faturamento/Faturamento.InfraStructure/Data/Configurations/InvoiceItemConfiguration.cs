using Faturamento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faturamento.InfraStructure.Data.Configurations;

public sealed class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("invoice_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).UseIdentityByDefaultColumn();
        builder.Property(item => item.InvoiceId).IsRequired();
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(item => item.ProductDescription).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();

        builder.HasIndex(item => new { item.InvoiceId, item.ProductId })
            .IsUnique()
            .HasDatabaseName("ux_invoice_items_product");
    }
}
