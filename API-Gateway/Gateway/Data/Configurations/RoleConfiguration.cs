using Gateway.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gateway.Data.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).UseIdentityByDefaultColumn();
        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(200);
        builder.Property(r => r.Active).HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("ux_roles_name");

        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasData(DefaultRoles.All.Select(role => new
        {
            role.Id,
            role.Name,
            Description = (string?)role.Description,
            Active = true,
            CreatedAt = DefaultRoles.SeededAt
        }).ToArray());
    }
}
