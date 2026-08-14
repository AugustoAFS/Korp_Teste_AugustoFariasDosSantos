using Gateway.Security.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gateway.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;
    private const int DelayInSeconds = 3;
    private const string DatabaseDoesNotExist = "3D000";
    private const string AuthenticationFailure = "28";

    public static async Task PrepareDatabase(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IArgon2idHasher>();

        await WaitForServer(context, app.Logger);
        await ApplyMigrations(context, app.Logger);
        await context.EnsureDefaultRoles(app.Logger);
        await context.EnsureAdmin(app.Configuration, hasher, app.Logger);
    }

    private static async Task WaitForServer(GatewayDbContext context, ILogger logger)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();

                logger.LogInformation("Conexão com o banco estabelecida");
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == DatabaseDoesNotExist)
            {
                logger.LogInformation("Servidor disponível, banco ainda não existe");
                return;
            }
            catch (PostgresException ex) when (ex.SqlState.StartsWith(AuthenticationFailure, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Credenciais recusadas pelo servidor de banco. Verifique ConnectionStrings:GatewayDb.", ex);
            }
            catch (NpgsqlException ex)
            {
                logger.LogWarning(
                    "Banco indisponível na tentativa {Tentativa} de {Total}: {Motivo}",
                    attempt, MaxAttempts, ex.Message);

                if (attempt == MaxAttempts)
                    throw new InvalidOperationException(
                        $"Sem conexão com o banco após {MaxAttempts} tentativas. " +
                        "Verifique ConnectionStrings:GatewayDb e se o container gateway-db está no ar.", ex);

                await Task.Delay(TimeSpan.FromSeconds(DelayInSeconds));
            }
        }
    }

    private static async Task ApplyMigrations(GatewayDbContext context, ILogger logger)
    {
        if (!await context.Database.CanConnectAsync())
        {
            logger.LogInformation("Criando o banco e aplicando as migrations");

            await context.Database.MigrateAsync();

            logger.LogInformation("Banco criado e migrations aplicadas");
            return;
        }

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();

        if (pending.Length == 0)
        {
            logger.LogInformation("Banco em dia, nenhuma migration pendente");
            return;
        }

        logger.LogInformation(
            "Aplicando {Total} migration(s): {Migrations}",
            pending.Length, string.Join(", ", pending));

        await context.Database.MigrateAsync();

        logger.LogInformation("Migrations aplicadas");
    }
}
