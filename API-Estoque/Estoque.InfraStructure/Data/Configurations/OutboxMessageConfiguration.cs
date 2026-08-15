using Estoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoque.InfraStructure.Data.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id).IsClustered(false);

        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).HasColumnType("varchar(100)").IsRequired();
        builder.Property(m => m.Payload).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(m => m.PublishedAt).HasColumnType("datetimeoffset(3)");
        builder.Property(m => m.Attempts).IsRequired();
        builder.Property(m => m.LastError).HasColumnType("nvarchar(max)");

        builder.HasIndex(m => m.CreatedAt)
            .IsClustered()
            .HasDatabaseName("ix_outbox_messages_created_at");

        builder.HasIndex(m => new { m.PublishedAt, m.Attempts, m.CreatedAt })
            .HasFilter("[published_at] IS NULL")
            .HasDatabaseName("ix_outbox_messages_pending");
    }
}
