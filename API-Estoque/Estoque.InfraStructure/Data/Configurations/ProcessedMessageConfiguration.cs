using Estoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoque.InfraStructure.Data.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageId).ValueGeneratedNever();
        builder.Property(m => m.Type).HasColumnType("varchar(100)").IsRequired();
        builder.Property(m => m.ProcessedAt).HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(m => m.OutcomeType).HasColumnType("varchar(100)");
        builder.Property(m => m.OutcomePayload).HasColumnType("nvarchar(max)");
    }
}
