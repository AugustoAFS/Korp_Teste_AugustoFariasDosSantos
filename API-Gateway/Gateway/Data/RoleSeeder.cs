using Gateway.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gateway.Data;

public static class RoleSeeder
{
    private const string UniqueViolation = "23505";

    public static async Task EnsureDefaultRoles(this GatewayDbContext context, ILogger logger)
    {
        var stored = await context.Roles
            .IgnoreQueryFilters()
            .Select(role => new { role.Name, role.Active, role.DeletedAt })
            .ToArrayAsync();

        var unavailable = stored
            .Where(role => !role.Active || role.DeletedAt is not null)
            .Select(role => role.Name)
            .ToArray();

        if (unavailable.Length > 0)
            logger.LogWarning(
                "Perfis padrão indisponíveis para novos usuários: {Perfis}",
                string.Join(", ", unavailable));

        var missing = DefaultRoles.All
            .Where(role => !stored.Any(s => string.Equals(s.Name, role.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (missing.Length == 0)
        {
            logger.LogInformation("Perfis padrão já cadastrados");
            return;
        }

        foreach (var role in missing)
            context.Roles.Add(new Role(role.Id, role.Name, role.Description));

        try
        {
            await context.SaveChangesAsync();

            logger.LogInformation(
                "Perfis padrão inseridos: {Perfis}",
                string.Join(", ", missing.Select(role => role.Name)));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            logger.LogInformation("Perfis padrão inseridos por outra instância em paralelo");
        }
    }
}
