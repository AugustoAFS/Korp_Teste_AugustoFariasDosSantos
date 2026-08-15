using Faturamento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faturamento.InfraStructure.Data.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Ignore(i => i.Printing);
        builder.Ignore(i => i.Editable);

        builder.Property(i => i.Id).UseIdentityByDefaultColumn();
        builder.Property(i => i.Number).IsRequired();
        builder.Property(i => i.Status).HasConversion<short>().IsRequired();
        builder.Property(i => i.IssuedByUserId).IsRequired();
        builder.Property(i => i.IssuedByUserName).HasMaxLength(150).IsRequired();
        builder.Property(i => i.ClosedAt);
        builder.Property(i => i.ProcessingId);
        builder.Property(i => i.ProcessingStartedAt);
        builder.Property(i => i.LastError);

        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Invoice.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(i => i.Number)
            .IsUnique()
            .HasDatabaseName("ux_invoices_number");

        builder.HasIndex(i => i.ProcessingStartedAt)
            .HasFilter("processing_id IS NOT NULL AND last_error IS NULL")
            .HasDatabaseName("ix_invoices_processing");

        builder.HasIndex(i => new { i.IssuedByUserId, i.CreatedAt })
            .HasDatabaseName("ix_invoices_user");

        builder.HasQueryFilter(i => i.DeletedAt == null);
    }
}
