using Gateway.Config;
using Gateway.Models;
using Gateway.Models.Enums;
using Gateway.Security.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Data;

public static class AdminSeeder
{
    private const string Section = "Seed:Admin";
    private const int MinimumPasswordLength = 8;
    private const long AdministratorId = (long)DefaultRole.Administrador;

    public static async Task EnsureAdmin(
        this GatewayDbContext context,
        IConfiguration configuration,
        IArgon2idHasher hasher,
        ILogger logger)
    {
        var seed = configuration.GetSection(Section);

        if (!seed.Exists())
        {
            logger.LogInformation("Administrador inicial não configurado, seed ignorado");
            return;
        }

        var name = RequiredSetting.Of(seed, "Name", 3);
        var email = RequiredSetting.Of(seed, "Email", 5);
        var password = RequiredSetting.Of(seed, "Password", MinimumPasswordLength);

        var stored = await context.Users
            .IgnoreQueryFilters()
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user => user.Email == email);

        if (stored is not null)
        {
            await GrantAdministrator(context, stored, logger);
            return;
        }

        var admin = new User(name, email, hasher.Hash(password));

        admin.AssignRole(AdministratorId);

        context.Users.Add(admin);

        await context.SaveChangesAsync();

        logger.LogWarning(
            "Administrador inicial {Email} criado a partir de Seed:Admin. Troque a senha antes de expor a aplicação",
            email);
    }

    private static async Task GrantAdministrator(GatewayDbContext context, User admin, ILogger logger)
    {
        if (admin.Roles.Any(link => link.RoleId == AdministratorId))
        {
            logger.LogInformation("Administrador inicial já cadastrado");
            return;
        }

        admin.AssignRole(AdministratorId);

        await context.SaveChangesAsync();

        logger.LogWarning("Perfil Administrador devolvido ao usuário inicial");
    }
}
