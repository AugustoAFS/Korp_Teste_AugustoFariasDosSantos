using Faturamento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faturamento.InfraStructure.Data.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.PublishedAt);
        builder.Property(m => m.Attempts).IsRequired();
        builder.Property(m => m.LastError);

        builder.HasIndex(m => m.CreatedAt)
            .HasFilter("published_at IS NULL")
            .HasDatabaseName("ix_outbox_messages_pending");
    }
}
